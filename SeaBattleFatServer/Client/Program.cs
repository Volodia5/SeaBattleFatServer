using ConnectionLibrary;
using ConnectionLibrary.Entity;
using ConnectionLibrary.Tools;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {

            TcpClient client = ConnectionTools.Connect();

            if (client != null)
                Play(client);

            Console.ReadLine();

            return;

        }

        private static void Play(TcpClient client)
        {
            try
            {
                Console.OutputEncoding = Encoding.Unicode;

                const int FielsSize = 10;

                string team = ConnectionTools.GetResponce(client).Value;
                Console.WriteLine("Вы играете за " + team);

                if (team == ConstantData.PlayerChars.First)
                    Console.WriteLine("Ожидаем второго игрока");

                string gameStatus = ConnectionTools.GetResponce(client).Value;
                bool isPlaying = true;

                while (isPlaying)
                {
                    Console.Clear();
                    Console.WriteLine("Вы играете за " + team);

                    switch (gameStatus)
                    {
                        case ConstantData.GameStates.Go:
                            PrintField(client, team);
                            Step(client, FielsSize);
                            Request request = new Request() { Command = ConstantData.Commands.EndStep };
                            ConnectionTools.SendRequest(client, request);
                            break;
                        case ConstantData.GameStates.Wait:
                            Console.WriteLine("Ждем ход противника");
                            break;
                    }

                    gameStatus = ConnectionTools.GetResponce(client).Value;

                    if (ConnectionTools.GetResponce(client).Value == ConstantData.GameStates.End)
                        isPlaying = false;
                }

                Console.Clear();
                Console.WriteLine("Ваше поле: ");
                Console.WriteLine(string.Join("Поле соперника:", GetFields(client)));

                string winner = ConnectionTools.GetResponce(client).Value;
                Console.WriteLine("Победил - " + winner);
            }
            catch (Exception exception)
            {
                Console.WriteLine("ERROR: " + exception.Message);
            }
        }

        private static void PrintField(TcpClient client, string team)
        {
            if (team == "1")
            {
                Console.WriteLine("Ваше поле: ");
                Console.WriteLine(string.Join("Поле соперника:", GetFields(client)));
            }
            else if (team == "2")
            {
                Console.WriteLine("Ваше поле: ");
                Console.WriteLine(string.Join("Поле соперника:", GetFields(client)));
            }
        }

        private static void Step(TcpClient client, int fielsSize)
        {
            do
            {
                string i = GetClumpValue("i", 1, fielsSize);
                string j = GetClumpValue("j", 1, fielsSize);

                Request request = new Request() { Command = ConstantData.Commands.Step, Parameters = new string[] { i, j } };
                ConnectionTools.SendRequest(client, request);
            } while (ConnectionTools.GetResponce(client).Result == ConstantData.ResponceResults.Error);
        }

        private static string GetClumpValue(string valueName, int start, int end)
        {
            Console.WriteLine($"Введите {valueName} от {start} до {end}");
            return Console.ReadLine();
        }

        private static string DeserializeField(string data)
        {
            string[] lines = data.Split(':', '/');
            return string.Join("\n", lines);
        }

        private static string GetFields(TcpClient client)
        {
            Request request = new Request() { Command = ConstantData.Commands.GetFields };
            ConnectionTools.SendRequest(client, request);

            return DeserializeField(ConnectionTools.GetResponce(client).Value);
        }
    }
}
