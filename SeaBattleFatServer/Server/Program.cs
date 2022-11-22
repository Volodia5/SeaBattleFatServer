using ConnectionLibrary;
using ConnectionLibrary.Entity;
using ConnectionLibrary.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private const int RowCount = 10;
        private const int ColumnCount = 10;

        private static void Main(string[] args)
        {
            int currentStepNumber = 0;
            CellValue[,] field = new CellValue[RowCount, ColumnCount];
            ClearField(field);
            field = RandomField(field);
            TcpListener server = ConnectionTools.GetListener();

            GameStatus gameStatus = GameStatus.Play;

            Logger.Log("SERVER STARTED");

            TcpClient playerFirstCrossClient = AcceptClient(server, ConstantData.PlayerChars.First);
            TcpClient playerSecondClient = AcceptClient(server, ConstantData.PlayerChars.Second);

            ConnectionTools.SendResponce(playerFirstCrossClient, ConstantData.GameStates.Go);
            ConnectionTools.SendResponce(playerSecondClient, ConstantData.GameStates.Wait);

            while (gameStatus == GameStatus.Play)
            {
                currentStepNumber++;

                SetStepParameters(currentStepNumber, playerFirstCrossClient, playerSecondClient,
                    out string currentValue, out TcpClient currentPlayer,
                    out string stateCrossAfter, out string stateZeroAfter);

                bool isStepEnd = false;

                while (isStepEnd == false)
                {
                    Request request = ConnectionTools.GetRequest(currentPlayer);

                    if (request.Command == ConstantData.Commands.Step)
                        Logger.Log($"STEP №{currentStepNumber} by {currentValue}: {request.Parameters[0]},{request.Parameters[1]}");

                    switch (request.Command)
                    {
                        case ConstantData.Commands.Step:
                            ProcessCommandStep(currentPlayer, field, request.Parameters, currentValue);
                            break;
                        case ConstantData.Commands.EndStep:
                            isStepEnd = true;
                            ProcessCommandEndStep(field, playerFirstCrossClient, playerSecondClient, stateCrossAfter, stateZeroAfter);
                            break;
                        case ConstantData.Commands.GetField:
                            ProcessCommandGetField(currentPlayer, field);
                            break;
                    }
                }

                gameStatus = GetGameStatus(field);
                Logger.Log("=========" + GetGameStatusString(gameStatus));
            }

            ProcessCommandGetField(playerFirstCrossClient, field);
            ProcessCommandGetField(playerSecondClient, field);

            ConnectionTools.SendResponce(playerFirstCrossClient, GetGameStatusString(gameStatus));
            ConnectionTools.SendResponce(playerSecondClient, GetGameStatusString(gameStatus));

            playerFirstCrossClient.Close();
            playerSecondClient.Close();

            server.Stop();

            Logger.Log("SERVER STOPED");
            Console.ReadLine();
        }

        private static string GetGameStatusString(GameStatus status)
        {
            switch (status)
            {
                case GameStatus.Play:
                    return ConstantData.GameStatus.Play;
                case GameStatus.WinFirst:
                    return ConstantData.GameStatus.WinFirst;
                case GameStatus.WinSecond:
                    return ConstantData.GameStatus.WinSecond;
            }

            return string.Empty;
        }

        private static void ProcessCommandEndStep(CellValue[,] field, TcpClient playerCross, TcpClient playerZero, string stateCrossAfter, string stateZeroAfter)
        {
            ConnectionTools.SendResponce(playerCross, stateCrossAfter);
            ConnectionTools.SendResponce(playerZero, stateZeroAfter);

            bool isEndGame = GetGameStatus(field) == GameStatus.Play;
            string endResult = isEndGame ? ConstantData.ResponceResults.Ok : ConstantData.GameStates.End;

            ConnectionTools.SendResponce(playerCross, endResult);
            ConnectionTools.SendResponce(playerZero, endResult);
        }

        private static void ProcessCommandGetField(TcpClient player, CellValue[,] field)
        {
            ConnectionTools.SendResponce(player, GetField(field));
        }

        private static string GetField(CellValue[,] field)
        {
            string textField = string.Empty;

            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    switch (field[i, j])
                    {
                        case CellValue.Empty:
                            textField += " ";
                            break;
                        case CellValue.Ship:
                            textField += ConstantData.PlayerChars.Ship;
                            break;
                    }
                }

                textField += ":";
            }

            return textField;
        }

        private static void ProcessCommandStep(TcpClient player, CellValue[,] field, string[] coordinates, string value)
        {
            bool stepResult;

            int.TryParse(coordinates[0], out int i);
            int.TryParse(coordinates[1], out int j);

            stepResult = TryMakeStep(field, i, j, value == ConstantData.PlayerChars.First ? CellValue.First : CellValue.Second);

            string responce = stepResult ? value : string.Empty;
            ConnectionTools.SendResponce(player, responce);
        }

        private static bool TryMakeStep(CellValue[,] field, int i, int j, CellValue value)
        {
            if (i <= 0 || j <= 0 || i > RowCount || j > ColumnCount || field[i - 1, j - 1] != CellValue.Empty)
                return false;

            field[i - 1, j - 1] = value;
            return true;
        }

        private static GameStatus GetGameStatus(CellValue[,] field)
        {
            if (CheckWinCondition(field, CellValue.First))
                return GameStatus.WinFirst;

            if (CheckWinCondition(field, CellValue.Second))
                return GameStatus.WinSecond;

            if (HasEmpty(field) == false)
                return GameStatus.Draw;

            return GameStatus.Play;
        }

        private static bool CheckWinCondition(CellValue[,] field, CellValue value)
        {
            return field[0, 0] == value && field[0, 1] == value && field[0, 2] == value ||
                    field[1, 0] == value && field[1, 1] == value && field[1, 2] == value ||
                    field[2, 0] == value && field[2, 1] == value && field[2, 2] == value ||

                    field[0, 0] == value && field[1, 0] == value && field[2, 0] == value ||
                    field[0, 1] == value && field[1, 1] == value && field[2, 1] == value ||
                    field[0, 2] == value && field[1, 2] == value && field[2, 2] == value ||

                    field[0, 0] == value && field[1, 1] == value && field[2, 2] == value ||
                    field[2, 0] == value && field[1, 1] == value && field[0, 2] == value;
        }

        private static bool HasEmpty(CellValue[,] field)
        {
            for (int i = 0; i < RowCount; i++)
                for (int j = 0; j < ColumnCount; j++)
                    if (field[i, j] == CellValue.Empty)
                        return true;

            return false;
        }

        private static void SetStepParameters(int currentStep, TcpClient playerCrossClient, TcpClient playerZeroClient,
            out string currentValue, out TcpClient currentPlayer, out string stateCrossAfter, out string stateZeroAfter)
        {
            if (currentStep % 2 == 0)
            {
                currentValue = ConstantData.PlayerChars.Second;
                currentPlayer = playerZeroClient;
                stateCrossAfter = ConstantData.GameStates.Go;
                stateZeroAfter = ConstantData.GameStates.Wait;
            }
            else
            {
                currentValue = ConstantData.PlayerChars.First;
                currentPlayer = playerCrossClient;
                stateZeroAfter = ConstantData.GameStates.Go;
                stateCrossAfter = ConstantData.GameStates.Wait;
            }
        }

        private static TcpClient AcceptClient(TcpListener server, string teame)
        {
            TcpClient player = server.AcceptTcpClient();
            Logger.Log($"player {teame} connected from { player.Client.RemoteEndPoint}");
            ConnectionTools.SendResponce(player, teame);

            return player;
        }

        private static void ClearField(CellValue[,] field)
        {
            for (int i = 0; i < RowCount; i++)
                for (int j = 0; j < ColumnCount; j++)
                    field[i, j] = CellValue.Empty;
        }

        private static CellValue[,] RandomField(CellValue[,] field)
        {
            int shipCount = 5;
            Random random = new Random();
            bool isRandoming = true;

            while (isRandoming)
            {

                for (int i = 0; i < shipCount; i++)
                {
                    int iParameter = random.Next(0, 9);
                    int jParameter = random.Next(0, 9);

                    if (field[iParameter, jParameter] != CellValue.Ship)
                    {
                        field[iParameter, jParameter] = CellValue.Ship;
                    }
                    else
                    {
                        i--;
                    }

                }

                isRandoming = false;
            }

            return field;
        }
    }


    public enum CellValue
    {
        Ship = 'X',
        Empty = '.',
        First = '1',
        FirstHit = '⓵',
        Second = '2',
        SecondHit = '⓶'
    }

    public enum GameStatus
    {
        Play = -1,
        Draw = 0,
        WinFirst = 1,
        WinSecond = 2
    }
}
