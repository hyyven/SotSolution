using System;
using System.Net.Sockets;
using System.Threading.Tasks;

public static class ServerGestion
{
    private static TcpClient tcp_client = null;
    private static NetworkStream network_stream = null;
    private static bool is_connected = false;
    private static readonly int timingout_ms = 30 * 1000; // 30 seconds timeout

    public static async Task<bool> ft_connect_to_server(string host, int port)
    {
        Task connectTask;
        Task timeoutTask;
        Task receiveTask;
        Task completedTask;
        bool accepted;

        while (true)
        {
            try
            {
                Console.WriteLine($"Attempting to connect to {host}:{port}...");

                // clean up previous connection
                if (tcp_client != null)
                {
                    tcp_client.Close();
                    tcp_client = null;
                }
                tcp_client = new TcpClient();

                // Connect with timeout
                connectTask = tcp_client.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(timingout_ms)) != connectTask)
                {
                    Console.WriteLine("Connection timeout, retrying in 5 seconds...");
                    tcp_client?.Close();
                    tcp_client = null;
                    await Task.Delay(timingout_ms);
                    continue;
                }
                if (!tcp_client.Connected)
                {
                    Console.WriteLine("Failed to establish TCP connection, retrying in 5 seconds...");
                    await Task.Delay(timingout_ms);
                    continue;
                }
                network_stream = tcp_client.GetStream();

                // wait for server response
                Console.WriteLine("Waiting for server acceptance...");
                timeoutTask = Task.Delay(timingout_ms);
                receiveTask = ft_wait_for_acceptance();
                completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("Server did not send ACCEPTED within 5 seconds, retrying connection in 5 seconds...");
                    ft_disconnect_from_server();
                    await Task.Delay(timingout_ms);
                    continue;
                }
                accepted = await (Task<bool>)receiveTask;
                if (accepted)
                {
                    is_connected = true;
                    Console.WriteLine($"Successfully connected {host}:{port}");
                    return true;
                }
                else
                {
                    Console.WriteLine("Server did not accept connection, retrying in 5 seconds...");
                    ft_disconnect_from_server();
                    await Task.Delay(timingout_ms);
                    continue;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to connect to server: {exception.Message}, retrying in 5 seconds...");
                ft_disconnect_from_server();
                await Task.Delay(timingout_ms);
            }
        }
    }

    private static async Task<bool> ft_wait_for_acceptance()
    {
        byte[] buffer;
        int bytes_read;
        string response;

        try
        {
            if (network_stream == null)
                return false;
            buffer = new byte[1024];
            bytes_read = await network_stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytes_read == 0)
                return false;
            response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytes_read);
            Console.WriteLine($"Received server response: {response}");
            return response.Contains("ACCEPTED");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error waiting for acceptance: {exception.Message}");
            return false;
        }
    }

    public static async Task<bool> ft_send_data(string data)
    {
        byte[] data_bytes;

        try
        {
            if (!is_connected || network_stream == null)
            {
                Console.WriteLine("Not connected to server");
                return false;
            }
            data_bytes = System.Text.Encoding.UTF8.GetBytes(data);
            await network_stream.WriteAsync(data_bytes, 0, data_bytes.Length);
            Console.WriteLine($"Sent: {data}");
            return true;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error sending data: {exception.Message}");
            return false;
        }
    }

    public static async Task<string> ft_receive_data()
    {
        byte[] buffer;
        int bytes_read;
        string received_data;

        try
        {
            if (!is_connected || network_stream == null)
            {
                Console.WriteLine("Not connected to server");
                return null;
            }
            buffer = new byte[4096];
            bytes_read = await network_stream.ReadAsync(buffer, 0, buffer.Length);
            received_data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytes_read);
            // Console.WriteLine($"Received: {received_data}");
            return received_data;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error receiving data: {exception.Message}");
            return null;
        }
    }

    public static void ft_disconnect_from_server()
    {
        try
        {
            if (network_stream != null)
            {
                network_stream.Close();
                network_stream = null;
            }
            if (tcp_client != null)
            {
                tcp_client.Close();
                tcp_client = null;
            }
            is_connected = false;
            Console.WriteLine("Disconnected from server");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error disconnecting: {exception.Message}");
        }
    }

    public static bool ft_is_connected()
    {
        return is_connected && tcp_client != null && tcp_client.Connected;
    }

    public static async Task ft_listen_for_messages()
    {
        string message_buffer = "";
        string received_data;
        string[] messages;
        string message;

        while (ft_is_connected())
        {
            received_data = await ft_receive_data();
            if (received_data != null)
            {
                message_buffer += received_data;
                messages = message_buffer.Split('\n');
                for (int i = 0; i < messages.Length - 1; i++)
                {
                    message = messages[i].Trim();
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (message.Contains("ACCEPTED"))
                        {
                            continue; // a afficher
                        }
                        // Console.WriteLine($"Processing message: {message}");
                        process_message(message);
                    }
                }
                message_buffer = messages[messages.Length - 1];
            }
        }
    }

    private static void process_message(string message)
    {
        Dictionary<string, object> json_message;
        System.Text.Json.JsonElement gamertags_element;
        System.Text.Json.JsonElement colors_element;
        System.Text.Json.JsonElement mores_element;
        int i;
        byte[] d_data;

        try
        {
            // Console.WriteLine($"Processing message: {message}");
            json_message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(message);
            if (json_message != null &&
                json_message.ContainsKey("GAMERTAGS") &&
                json_message.ContainsKey("COLORS") &&
                json_message.ContainsKey("MORES"))
            {
                gamertags_element = (System.Text.Json.JsonElement)json_message["GAMERTAGS"];
                colors_element = (System.Text.Json.JsonElement)json_message["COLORS"];
                mores_element = (System.Text.Json.JsonElement)json_message["MORES"];
                i = 0;
                foreach (var gamertag_item in gamertags_element.EnumerateArray())
                {
                    // Check if gamertag is already in the list
                    // if (Utils.gt_info.Any(x => x.gamertag == gamertag_item.GetString()))
                    // {
                    //     i++;
                    //     continue;
                    // }
                    Utils.gt_info.Add((gamertag_item.GetString(), colors_element[i].GetString(), mores_element[i].GetString()));
                    // print a enlever (ou pas sah)
                    Console.WriteLine($"Added gamertag: {gamertag_item.GetString()}, color: {colors_element[i].GetString()}, more: {mores_element[i].GetString()}");
                    i++;
                }
            }
            else if (json_message != null && json_message.ContainsKey("START_DOWNLOAD"))
            {
                Utils.f = new FileStream(Utils.path_db, FileMode.Create, FileAccess.Write);
                if (json_message.ContainsKey("SIZE"))
                {
                    Utils.f_size = ((System.Text.Json.JsonElement)json_message["SIZE"]).GetUInt64();
                }
                Console.WriteLine($"Download started, file size: {Utils.f_size} bytes");
            }
            else if (json_message != null && Utils.f != null && json_message.ContainsKey("DOWNLOAD"))
            {
                d_data = Convert.FromBase64String(((System.Text.Json.JsonElement)json_message["DOWNLOAD"]).GetString());
                Utils.f.Write(d_data, 0, d_data.Length);
                // Console.WriteLine($"Received {d_data.Length} bytes of data");
            }
            else if (json_message != null && Utils.f != null && json_message.ContainsKey("END_DOWNLOAD"))
            {
                Utils.f.Close();
                Utils.f = null;
                Utils.f_size = 0;
                Console.WriteLine("Download completed");
            }
            else
            {
                Console.WriteLine($"Unknown message format: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error process message: {ex.Message}");
            Console.WriteLine($"Message content: {message}");
        }
    }
}