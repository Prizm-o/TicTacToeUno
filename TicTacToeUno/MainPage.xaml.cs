using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Microsoft.UI;

namespace TicTacToeUno;

public sealed partial class MainPage : Page
{
    private HubConnection connection;
    Dictionary<string, Button> dict = new Dictionary<string, Button>();

    public MainPage()
    {
        this.InitializeComponent();
        try
        {
        #if __ANDROID__
            string url = "https://10.0.2.2:7064/GameHub";
        #else
            string url = "https://localhost:7064/gamehub";
        #endif
            connection = new HubConnectionBuilder()
            #if __ANDROID__
                    .WithUrl(url, options =>
                    {
                        options.HttpMessageHandlerFactory = (handler) =>
                        {
                            // Игнорируем проверку сертификата для самоподписанного сертификата (только для разработки)
                            if (handler is HttpClientHandler clientHandler)
                            {
                                clientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
                            }
                            return handler;
                        };
                    })
            #else
                    .WithUrl(url)
            #endif
                    //.WithAutomaticReconnect()
                    .Build();
            
            connection.Closed += async (error) =>
            {
                System.Threading.Thread.Sleep(5000);
                await connection.StartAsync();
            };
        }
        catch (Exception ex)
        {
            listBox1.Items.Add("Ошибка при создании подключения - " + ex.Message);
        }
        dict["btnCell0"] = btnCell0;
        dict["btnCell1"] = btnCell1;
        dict["btnCell2"] = btnCell2;
        dict["btnCell3"] = btnCell3;
        dict["btnCell4"] = btnCell4;
        dict["btnCell5"] = btnCell5;
        dict["btnCell6"] = btnCell6;
        dict["btnCell7"] = btnCell7;
        dict["btnCell8"] = btnCell8;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {

        try
        {
            await connection.StartAsync();
            listBox1.Items.Add(connection.State);
            if (connection?.State == HubConnectionState.Connected)
            {
                textBox1.IsEnabled = true;
                SendBtn.IsEnabled = true;
                SendBtn.Click += SendBtn_Click;
            }
        }
        catch (Exception ex)
        {
            listBox1.Items.Add(connection.State);
            listBox1.Items.Add("Ошибка при установке соединения - " + ex.Message);
            while (connection?.State != HubConnectionState.Connected)
            {
                System.Threading.Thread.Sleep(5000);
                try
                {
                    await connection.StartAsync();
                    listBox1.Items.Add(connection.State);
                }
                catch
                {
                    listBox1.Items.Add("Повторная попытка подключения");                    
                }
            }
            
            if (connection.State == HubConnectionState.Connected)
            {
                textBox1.IsEnabled = true;
                SendBtn.IsEnabled = true;
                SendBtn.Click += SendBtn_Click;
                listBox1.Items.Add(connection.State);
            }
            
        }        

        connection.On<string>("ReceiveMessage", message =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                listBox1.Items.Add(listBox1.Items.Count + ". " + message);
                listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
            });
        });

        connection.On("StartGame", () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                listBox1.Items.Add(listBox1.Items.Count + ". " + "Сделайте ход");
                listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);

                for (int i = 0; i < 9; i++)
                {
                    var btnCell = dict[$"btnCell{i}"];
                    //var btnCell = FindControl<Button>(this, $"btnCell{i}");
                    if (btnCell != null)
                    {
                        btnCell.Background = new SolidColorBrush(Colors.LightGray);
                        btnCell.IsEnabled = true;
                        btnCell.Content = "";
                    }
                };
            });
        });

        connection.On("JoinGame", () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                textBox1.IsEnabled = false;
                SendBtn.Click -= SendBtn_Click;
                SendBtn.Click += ExitBtn_Click;
                SendBtn.Content = "Выйти";
            });
        });

        connection.On("ExitGame", () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                textBox1.IsEnabled = true;
                SendBtn.Click -= ExitBtn_Click;
                SendBtn.Click += SendBtn_Click;
                SendBtn.Content = "Присоединиться";
            });
        });

        connection.On<string>("UpdateScore", (message) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                labelScore.Text = message;
            });
        });

        connection.On<string[]>("UpdateBoard", (board) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateGameBoard(board);
            });
        });

        connection.On<string[]>("UpdateBoardViewers", (board) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateBoardViewers(board);
            });
        });

        connection.On("NextMove", () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NextMove();
            });
        });

        connection.On<string>("GameOver", message =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                listBox1.Items.Add(listBox1.Items.Count + ". " + message);
                listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
                EndGame();
            });
        });
    }

    public static T FindControl<T>(UIElement parent, string ControlName) where T : FrameworkElement
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.GetType() == typeof(T) && ((T)parent).Name == ControlName)
        {
            return (T)parent;
        }
        T result = null;
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            UIElement child = (UIElement)VisualTreeHelper.GetChild(parent, i);

            if (FindControl<T>(child, ControlName) != null)
            {
                result = FindControl<T>(child, ControlName);
                break;
            }
        }
        return result;
    }


    private async void btnCell_Click(object sender, RoutedEventArgs e)
    {
        Button button = sender as Button;
        int x = Convert.ToInt32(button.Tag.ToString());

        await connection.InvokeAsync("MakeMove", x, connection.ConnectionId);
    }

    private void NextMove() //Блокируем кнопки у того, кто ходил
    {
        for (int i = 0; i < 9; i++)
        {
            var button = dict[$"btnCell{i}"];
            //var button = FindControl<Button>(this, $"btnCell{i}");
            if (button != null)
            {
                button.IsEnabled = false;
                if (!button.IsEnabled)
                {
                    if (button.Content != "")
                    {
                        button.Background = new SolidColorBrush(Colors.AliceBlue);
                    }
                    else
                    {
                        button.Background = new SolidColorBrush(Colors.Gray);
                    }
                }
            }
        }
    }

    private void UpdateGameBoard(string[] board) //Обновляем информацию в клетках для игроков
    {
        for (int i = 0; i < 9; i++)
        {
            var button = dict[$"btnCell{i}"];
            //var button = FindControl<Button>(this, $"btnCell{i}");
            if (button != null)
            {
                button.Content = board[i] ?? "";
                button.IsEnabled = board[i] == null; // Блокируем кнопки после хода
                if (!button.IsEnabled)
                {
                    if (button.Content != "")
                    {
                        button.Background = new SolidColorBrush(Colors.AliceBlue);
                    }
                    else
                    {
                        button.Background = new SolidColorBrush(Colors.Gray);
                    }
                }
                else { button.Background = new SolidColorBrush(Colors.LightGray); }
            }
        }
    }

    private void UpdateBoardViewers(string[] board) //Обновляем информацию в клетках для смотрящих
    {
        // Обновите кнопки на форме в зависимости от состояния игрового поля
        for (int i = 0; i < 9; i++)
        {
            var button = dict[$"btnCell{i}"];
            //var button = FindControl<Button>(this, $"btnCell{i}");
            if (button != null)
            {
                button.Content = board[i] ?? "";
                button.IsEnabled = false; // Запрещаем снова нажимать на кнопки
                if (!button.IsEnabled)
                {
                    if (button.Content != "")
                    {
                        button.Background = new SolidColorBrush(Colors.AliceBlue);
                    }
                    else
                    {
                        button.Background = new SolidColorBrush(Colors.Gray);
                    }
                }
                else { button.Background = new SolidColorBrush(Colors.LightGray); }
            }
        }
    }

    private void EndGame()
    {
        for (int i = 0; i < 9; i++)
        {
            var button = dict[$"btnCell{i}"];
            //var button = FindControl<Button>(this, $"btnCell{i}");
            if (button != null)
            {
                button.Content = "";
                button.IsEnabled = false;
                button.Background = new SolidColorBrush(Colors.Gray);
            }
        }
    }

    private async void SendBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await connection.InvokeAsync("Connect", textBox1.Text, connection.ConnectionId);
        }
        catch (Exception ex)
        {
            listBox1.Items.Add(connection.State);
            listBox1.Items.Add(listBox1.Items.Count + ". " + ex.Message);
            listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
        }
    }

    private async void ExitBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await connection.InvokeAsync("Disconnect", connection.ConnectionId);
        }
        catch (Exception ex)
        {
            listBox1.Items.Add(connection.State);
            listBox1.Items.Add(listBox1.Items.Count + ". " + ex.Message);
            listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
        }
    }    
}
