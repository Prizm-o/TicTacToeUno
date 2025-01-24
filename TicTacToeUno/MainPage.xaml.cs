using System.Drawing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.UI;
using System;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;

namespace TicTacToeUno;

public partial class MainPage : Page
{
    private HubConnection connection;

    public MainPage()
    {
        this.InitializeComponent();
        connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7064/gamehub")
                .Build();

        connection.Closed += async (error) =>
        {
            System.Threading.Thread.Sleep(5000);
            await connection.StartAsync();
        };
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await connection.StartAsync();
            listBox1.Items.Add(connection.State);
        }
        catch (Exception ex)
        {
            listBox1.Items.Add(connection.State);
            listBox1.Items.Add(ex.Message);
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
                    var btnCell = FindControl<Button>(this, $"btnCell{i}");
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
                var sendBtn = FindControl<Button>(this, "SendBtn");
                sendBtn.Click -= SendBtn_Click;
                sendBtn.Click += ExitBtn_Click;
                sendBtn.Content = "Выйти";
            });
        });

        connection.On("ExitGame", () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                textBox1.IsEnabled = true;
                var sendBtn = FindControl<Button>(this, "SendBtn");
                sendBtn.Click -= ExitBtn_Click;
                sendBtn.Click += SendBtn_Click;
                sendBtn.Content = "Присоединиться";
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
                listBox1.Items.Add(listBox1.Items.Count +". "+ message);
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
            var button =  FindControl<Button>(this, $"btnCell{i}");
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
            var button = FindControl<Button>(this, $"btnCell{i}");
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
            var button = FindControl<Button>(this, $"btnCell{i}");
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
            var button = FindControl<Button>(this, $"btnCell{i}");
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
