using PokerConsoleApp.Controllers;
using PokerConsoleApp.Enums;
using PokerConsoleApp.Interfaces;
using PokerConsoleApp.Models;

namespace PokerConsoleApp.UI;

public class ConsoleRenderer
{

    private const int Width = 90;
    private static string Bar => new string('═', Width);

    private readonly GameController _controller;

    public ConsoleRenderer(GameController controller)
    {
        _controller = controller;
    }


    public void RunGame()
    {
        try
        {
            _controller.OnRoundChanged += HandleRoundChanged;
            _controller.OnHandWinnersDecided += HandleHandWinners;


            ShowSetupBanner();

            int playerCount = AskPlayerCount();
            RegisterPlayers(playerCount);

            int smallBlind = _controller.GetSmallBind();
            int bigBlind = _controller.GetBigBlind();

            ShowSetupConfirmation(smallBlind, bigBlind);
            Console.ReadLine();

            _controller.StartGame();

            while (_controller.GetGameState() != GameState.GameOver)
            {
                if (_controller.GetGameState() == GameState.HandComplete)
                {
                    _controller.StartNexthand();
                    continue;
                }

                IPlayer activePlayer = _controller.GetCurrentPlayer();
                ShowPlayerTransitionScreen(activePlayer.Name);
                WaitForEnter();

                DrawPlayerView(activePlayer);
                ProcessTurn(activePlayer);
            }
        }
        finally
        {
            _controller.OnRoundChanged -= HandleRoundChanged;
            _controller.OnHandWinnersDecided -= HandleHandWinners;
        }

        ShowGameOver(_controller.GetGameWinner());
    }

    private void HandleRoundChanged(GameRound round)
    {
        if (round == GameRound.PreFlop || round == GameRound.Showdown)
        {
            return;
        }

        ShowRoundTransition(round, _controller.GetCommunityCards());
        WaitForEnter();
    }

    private void HandleHandWinners(List<IPlayer> winners)
    {
        List<IPot> pots = new List<IPot>();
        IPot? mainPot = _controller.GetMainPot();
        if (mainPot != null)
        {
            pots.Add(mainPot);
        }
        pots.AddRange(_controller.GetSidePots());

        ShowHandResult(winners, pots);
        WaitForEnter();
    }

    private int AskPlayerCount()
    {
        int playerCount = 0;
        while (playerCount < 2 || playerCount > 10)
        {
            Console.Write("\nMasukkan jumlah pemain (2-10): ");
            string input = Console.ReadLine() ?? string.Empty;

            if (!int.TryParse(input, out playerCount) || playerCount < 2 || playerCount > 10)
            {
                Console.WriteLine("Jumlah pemain harus antara 2-10!");
            }
        }
        return playerCount;
    }

    private void RegisterPlayers(int playerCount)
    {
        List<string> registeredNames = new List<string>();

        for (int i = 1; i <= playerCount; i++)
        {
            Console.WriteLine($"\n--- Pemain {i} ---");

            string name = "";
            bool validName = false;

            while (!validName)
            {
                Console.Write($"Masukkan nama pemain {i}: ");
                name = Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("Nama tidak boleh kosong!");
                    continue;
                }

                if (registeredNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Nama '{name}' sudah digunakan! Pilih nama lain.");
                    continue;
                }

                validName = true;
            }

            registeredNames.Add(name);
            _controller.AddPlayer(name, 1000);
        }
    }

    public void WaitForEnter(string message = "")
    {
        if (!string.IsNullOrEmpty(message))
        {
            Console.WriteLine(message);
        }
        Console.ReadLine();
    }

    public void ProcessTurn(IPlayer player)
    {
        List<BettingAction> availableActions = _controller.GetAvailableBettingAction(player);

        int selectedIndex = -1;
        while (selectedIndex < 0 || selectedIndex >= availableActions.Count)
        {
            string userInput = Console.ReadLine() ?? string.Empty;

            if (int.TryParse(userInput, out int choiceNumber) && choiceNumber >= 1 && choiceNumber <= availableActions.Count)
            {
                selectedIndex = choiceNumber - 1;
            }
            else
            {
                Console.WriteLine("Input tidak valid! Masukkan angka yang tertera pada menu.");
                Console.Write($"Ketik angka opsi (1-{availableActions.Count}): ");
            }
        }

        BettingAction chosenAction = availableActions[selectedIndex];

        switch (chosenAction)
        {
            case BettingAction.Fold:
                _controller.Fold(player);
                break;

            case BettingAction.Check:
                _controller.Check(player);
                break;

            case BettingAction.Call:
                _controller.Call(player);
                break;

            case BettingAction.Raise:
                ProcessRaiseAction(player);
                break;

            case BettingAction.AllIn:
                _controller.AllIn(player);
                break;
        }
    }

    private void ProcessRaiseAction(IPlayer player)
    {
        int minRaise = _controller.GetMinRaise();
        int maxChips = _controller.GetPlayerChips(player) + _controller.GetPlayerCurrentBet(player);
        int raiseTargetAmount = 0;

        while (raiseTargetAmount < minRaise || raiseTargetAmount > maxChips)
        {
            Console.Write($"Masukkan total taruhan baru Anda ({minRaise} - {maxChips}): ");
            string userInput = Console.ReadLine() ?? string.Empty;

            if (int.TryParse(userInput, out int inputAmount))
            {
                raiseTargetAmount = inputAmount;
                if (raiseTargetAmount < minRaise)
                {
                    Console.WriteLine($"Nominal terlalu kecil! Minimal Raise adalah {minRaise}.");
                }
                else if (raiseTargetAmount > maxChips)
                {
                    Console.WriteLine("Chip Anda tidak mencukupi untuk melakukan Raise.");
                }
            }
            else
            {
                Console.WriteLine("Masukkan format angka yang valid!");
            }
        }

        _controller.Raise(player, raiseTargetAmount);
    }

    private void SetBorderColor() => Console.ForegroundColor = ConsoleColor.DarkCyan;

    private void DrawTop()
    {
        SetBorderColor();
        Console.WriteLine($"╔{Bar}╗");
        Console.ResetColor();
    }

    private void DrawBottom()
    {
        SetBorderColor();
        Console.WriteLine($"╚{Bar}╝");
        Console.ResetColor();
    }

    private void DrawDivider()
    {
        SetBorderColor();
        Console.WriteLine($"╠{Bar}╣");
        Console.ResetColor();
    }

    private void DrawLine(string content = "", ConsoleColor? color = null)
    {
        if (content.Length > Width)
        {
            content = content.Substring(0, Width);
        }

        SetBorderColor();
        Console.Write("║");
        Console.ResetColor();

        if (color.HasValue)
        {
            Console.ForegroundColor = color.Value;
        }
        Console.Write(content.PadRight(Width));
        if (color.HasValue)
        {
            Console.ResetColor();
        }

        SetBorderColor();
        Console.WriteLine("║");
        Console.ResetColor();
    }

    private void DrawCenteredLine(string content, ConsoleColor? color = null)
    {
        if (content.Length > Width)
        {
            content = content.Substring(0, Width);
        }
        int totalPad = Width - content.Length;
        int left = totalPad / 2;
        int right = totalPad - left;
        DrawLine(new string(' ', left) + content + new string(' ', right), color);
    }

    private void DrawCardsLine(string label, IEnumerable<ICard>? cards, string emptyText = "[ ]")
    {
        List<ICard> cardList = cards?.ToList() ?? new List<ICard>();

        SetBorderColor();
        Console.Write("║");
        Console.ResetColor();

        Console.Write(label);
        int length = label.Length;

        if (cardList.Count == 0)
        {
            Console.Write(emptyText);
            length += emptyText.Length;
        }
        else
        {
            foreach (ICard card in cardList)
            {
                string cardText = card.ToString();
                setCardColor(card.Suit);
                Console.Write(cardText);
                Console.ResetColor();
                Console.Write(" ");
                length += cardText.Length + 1;
            }
        }

        int pad = Math.Max(0, Width - length);
        Console.Write(new string(' ', pad));

        SetBorderColor();
        Console.WriteLine("║");
        Console.ResetColor();
    }


    public void ShowSetupBanner()
    {
        Console.Clear();
        DrawTop();
        DrawLine();
        DrawCenteredLine("♠ ♥ TEXAS HOLD'EM POKER ♦ ♣", ConsoleColor.Yellow);
        DrawLine();
        DrawBottom();
    }

    public void ShowSetupConfirmation(int smallBlind, int bigBlind)
    {
        Console.Clear();
        List<IPlayer> players = _controller.GetAllPlayers();

        DrawTop();
        DrawCenteredLine("GAME SIAP DIMULAI!", ConsoleColor.Green);
        DrawDivider();
        DrawLine($" Jumlah Pemain : {players.Count}");
        DrawLine($" Small Blind   : {smallBlind}      Big Blind : {bigBlind}");
        DrawLine();

        foreach (IPlayer p in players)
        {
            DrawLine($" • {p.Name,-12} : {_controller.GetPlayerChips(p)} chips");
        }

        DrawLine();
        DrawCenteredLine("Tekan ENTER untuk memulai permainan...", ConsoleColor.DarkGray);
        DrawBottom();
    }

    public void ShowPlayerTransitionScreen(string playerName)
    {
        Console.Clear();
        DrawTop();
        DrawLine();
        DrawCenteredLine($"GILIRAN: {playerName}", ConsoleColor.Yellow);
        DrawLine();
        DrawCenteredLine("Serahkan perangkat ke pemain tersebut");
        DrawLine();
        DrawCenteredLine("Tekan ENTER jika siap...", ConsoleColor.DarkGray);
        DrawLine();
        DrawBottom();
    }

    private const int TableWidth = 90;
    private const int SeatWidth = 17;
    private const int SeatsPerRow = 5;

    public void DrawPlayerView(IPlayer currentPlayer)
    {
        Console.Clear();

        List<IPlayer> allPlayers = _controller.GetAllPlayers();
        IPlayer dealer = _controller.GetDealer();

        int topCount = (allPlayers.Count + 1) / 2;
        List<IPlayer> topRow = allPlayers.Take(topCount).ToList();
        List<IPlayer> bottomRow = allPlayers.Skip(topCount).Reverse().ToList();

        //  Header (satu-satunya bagian berbingkai ║)
        DrawTop();
        DrawCenteredLine("♠ ♥  TEXAS HOLD'EM POKER  ♦ ♣", ConsoleColor.Yellow);
        DrawBottom();
        Console.WriteLine();

        //  Status bar (round & pot), terpisah dari meja
        DrawStatusBar();
        Console.WriteLine();

        //  Meja (area terbuka, tanpa border)
        DrawRule();
        DrawSeatsRowPlain(topRow, currentPlayer, dealer);
        // DrawRule();
        Console.WriteLine();
        Console.WriteLine();
        DrawCommunityCardsCentered();
        Console.WriteLine();
        Console.WriteLine();
        // DrawRule();
        DrawSeatsRowPlain(bottomRow, currentPlayer, dealer);
        DrawRule();
        Console.WriteLine();

        //  Panel aksi (berbingkai, seperti tombol aksi)
        DrawTop();
        int actionCount = DrawAvailableActions(currentPlayer);
        DrawBottom();

        Console.Write($"Pilih opsi (1-{actionCount}): ");
    }

    private void DrawRule()
    {
        SetBorderColor();
        Console.WriteLine(new string('─', TableWidth));
        Console.ResetColor();
    }

    private void DrawStatusBar()
    {
        string roundText = $" Round: {_controller.GetCurrentRound()}";
        string potText;

        potText = $"Total Pot: {_controller.GetTotalPotAmount()} Chips ";

        int pad = Math.Max(1, TableWidth - roundText.Length - potText.Length);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(roundText);
        Console.ResetColor();
        Console.Write(new string(' ', pad));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(potText);
        Console.ResetColor();
    }

    private void DrawCommunityCardsCentered()
    {
        List<ICard> community = _controller.GetCommunityCards();
        List<string> texts = new List<string>();

        for (int i = 0; i < 5; i++)
        {
            texts.Add(i < community.Count ? community[i].ToString() : "[ ]");
        }

        int totalLen = texts.Sum(t => t.Length) + (texts.Count - 1);
        int leftPad = Math.Max(0, (TableWidth - totalLen) / 2);

        Console.Write(new string(' ', leftPad));

        for (int i = 0; i < texts.Count; i++)
        {
            if (i < community.Count)
            {
                setCardColor(community[i].Suit);
            }
            else Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.Write(texts[i]);
            Console.ResetColor();

            if (i < texts.Count - 1)
            {
                Console.Write(" ");
            }
        }

        Console.WriteLine();
    }

    private void DrawSeatsRowPlain(List<IPlayer> rowPlayers, IPlayer currentPlayer, IPlayer dealer)
    {
        bool gameInProgress = _controller.GetGameState() == GameState.InProgress;

        List<string[]> seatLines = new List<string[]>();
        List<ConsoleColor?> seatColors = new List<ConsoleColor?>();

        foreach (IPlayer player in rowPlayers)
        {
            bool isSelf = player == currentPlayer;
            seatLines.Add(BuildSeatLines(player, isSelf, dealer));

            bool isTurn = isSelf && gameInProgress;
            bool isOut = player.Status == PlayerStatus.Folded || player.Status == PlayerStatus.Bust;

            seatColors.Add(isTurn ? ConsoleColor.Green : (isOut ? ConsoleColor.DarkGray : (ConsoleColor?)null));
        }

        while (seatLines.Count < SeatsPerRow)
        {
            seatLines.Add(new[] { "", "", "", "" });
            seatColors.Add(null);
        }

        int rowWidth = SeatsPerRow * SeatWidth + (SeatsPerRow - 1);
        int leftPad = Math.Max(0, (TableWidth - rowWidth) / 2);

        for (int line = 0; line < 4; line++)
        {
            Console.Write(new string(' ', leftPad));

            for (int s = 0; s < SeatsPerRow; s++)
            {
                string text = CenterText(seatLines[s][line], SeatWidth);

                ConsoleColor? currentColor = seatColors[s];
                if (currentColor.HasValue)
                {
                    Console.ForegroundColor = currentColor.Value;
                }
                Console.Write(text);
                if (seatColors[s].HasValue)
                {
                    Console.ResetColor();
                }

                if (s < SeatsPerRow - 1)
                {
                    Console.Write(" ");
                }
            }

            Console.WriteLine();
        }
    }

    private string[] BuildSeatLines(IPlayer player, bool isSelf, IPlayer dealer)
    {
        string marker = player == dealer ? "(D)" : "";

        string cards;
        if (isSelf)
        {
            cards = string.Join("", _controller.GetPlayerHoleCards(player).Select(c => c.ToString()));
        }
        else if (player.Status == PlayerStatus.Folded || player.Status == PlayerStatus.Bust)
        {
            cards = "";
        }
        else
        {
            cards = "[█][█]";
        }

        string name = player.Name.Length > SeatWidth ? player.Name.Substring(0, SeatWidth) : player.Name;

        string statusTag = player.Status switch
        {
            PlayerStatus.Folded => "(Fold)",
            PlayerStatus.AllIn => "(All-In)",
            PlayerStatus.Bust => "(Bust)",
            _ => ""
        };

        string info = statusTag != ""
            ? statusTag
            : $"{_controller.GetPlayerChips(player)}c";

        string bet = _controller.GetPlayerCurrentBet(player) > 0
            ? $"Bet:{_controller.GetPlayerCurrentBet(player)}"
            : "";

        return new[] { marker, cards, name, bet != "" ? $"{info} {bet}" : info };
    }

    private string CenterText(string content, int width)
    {
        if (content.Length > width)
        {
            content = content.Substring(0, width);
        }
        int totalPad = width - content.Length;
        int left = totalPad / 2;
        int right = totalPad - left;
        return new string(' ', left) + content + new string(' ', right);
    }

    private int DrawAvailableActions(IPlayer currentPlayer)
    {
        DrawLine(" AKSI", ConsoleColor.Cyan);

        List<BettingAction> actions = _controller.GetAvailableBettingAction(currentPlayer);
        for (int i = 0; i < actions.Count; i++)
        {
            string additionalInfo = "";
            if (actions[i] == BettingAction.Call)
            {
                additionalInfo = $" ({_controller.GetCallAmount(currentPlayer)} chips)";
            }
            else if (actions[i] == BettingAction.Raise)
            {
                additionalInfo = $" (Min: {_controller.GetMinRaise()} chips)";
            }

            DrawLine($"   [{i + 1}] {actions[i]}{additionalInfo}");
        }

        return actions.Count;
    }

    public void ShowRoundTransition(GameRound round, List<ICard> communityCards)
    {
        Console.Clear();
        string roundName = round switch
        {
            GameRound.Flop => "FLOP",
            GameRound.Turn => "TURN",
            GameRound.River => "RIVER",
            _ => round.ToString().ToUpper()
        };

        DrawTop();
        DrawLine();
        DrawCenteredLine($"*** {roundName} ***", ConsoleColor.Yellow);
        DrawLine();

        if (communityCards != null && communityCards.Count > 0)
        {
            DrawCardsLine(" Community Cards : ", communityCards);
            DrawLine();
        }

        DrawCenteredLine("Tekan ENTER untuk melanjutkan...", ConsoleColor.DarkGray);
        DrawLine();
        DrawBottom();
    }

    public void ShowHandResult(List<IPlayer> winners, List<IPot> pots)
    {
        Console.Clear();
        DrawTop();
        DrawLine();
        DrawCenteredLine("*** HASIL HAND ***", ConsoleColor.Yellow);
        DrawLine();

        // Tampilkan semua pemain aktif, urutkan berdasarkan hand rank
        List<IPlayer> activePlayers = _controller.GetAllPlayers()
            .Where(p => p.Status != PlayerStatus.Folded && p.Status != PlayerStatus.Bust)
            .ToList();

        // Urutkan berdasarkan hand rank (tertinggi ke terendah)
        List<IPlayer> sortedPlayers = activePlayers
            .OrderByDescending(p => _controller.GetPlayerHandRank(p))
            .ToList();

        // Tampilkan setiap pemain
        foreach (IPlayer player in sortedPlayers)
        {
            HandRank handRank = _controller.GetPlayerHandRank(player);
            List<ICard> bestFive = _controller.GetPlayerBestFiveCards(player);

            string cardsText = string.Join(" ", bestFive.Select(c => c.ToString()));
            bool isWinner = winners.Contains(player);

            string marker = isWinner ? "★" : " ";
            ConsoleColor color = isWinner ? ConsoleColor.Green : ConsoleColor.White;

            DrawLine($" {marker} {player.Name,-12} {handRank,-15} {cardsText}", color);
        }

        DrawDivider();

        // Tampilkan pemenang dan pot
        if (pots.Count == 1)
        {
            IPot pot = pots[0];
            int share = pot.TotalChips / winners.Count;

            foreach (IPlayer winner in winners)
            {
                DrawCenteredLine($"{winner.Name} memenangkan {share} Chips!", ConsoleColor.Green);
            }
        }
        else
        {
            for (int i = 0; i < pots.Count; i++)
            {
                IPot pot = pots[i];
                string potName = i == 0 ? "Main Pot" : $"Side Pot {i}";

                List<IPlayer> eligibleWinners = winners
                    .Where(w => pot.Contributions.ContainsKey(w))
                    .Distinct()
                    .ToList();
                if (eligibleWinners.Count == 0)
                {
                    continue;
                }

                int share = pot.TotalChips / eligibleWinners.Count;

                DrawLine($" {potName} ({pot.TotalChips} Chips):", ConsoleColor.Cyan);
                foreach (IPlayer winner in eligibleWinners)
                {
                    DrawLine($"    → {winner.Name} memenangkan {share} Chips", ConsoleColor.Green);
                }
            }
        }

        DrawLine();
        DrawCenteredLine("Tekan ENTER untuk lanjut...", ConsoleColor.DarkGray);
        DrawLine();
        DrawBottom();
    }

    public void ShowGameOver(IPlayer winner)
    {
        Console.Clear();
        DrawTop();
        DrawLine();
        DrawCenteredLine("*** GAME OVER ***", ConsoleColor.Yellow);
        DrawLine();
        DrawCenteredLine($"{winner.Name} MENANG!", ConsoleColor.Green);
        DrawLine();
        DrawCenteredLine($"Chips: {_controller.GetPlayerChips(winner)}");
        DrawLine();
        DrawCenteredLine("Selamat kepada pemenang!", ConsoleColor.Cyan);
        DrawLine();
        DrawBottom();
    }

    private void setCardColor(CardSuit suit)
    {
        Console.ForegroundColor = (suit == CardSuit.Hearts || suit == CardSuit.Diamonds)
            ? ConsoleColor.Red
            : ConsoleColor.White;
    }
}