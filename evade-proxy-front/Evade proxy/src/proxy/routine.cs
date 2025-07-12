using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

public static class Routine
{
    private static System.Timers.Timer send_timer = null;
    private static bool is_routine_active = false;

    public static void ft_start_routine(int interval_ms = 1 * 1000)
    {
        if (is_routine_active)
        {
            Console.WriteLine("Routine already active");
            return;
        }
        try
        {
            send_timer = new System.Timers.Timer(interval_ms);
            send_timer.Elapsed += ft_send_xuid_list;
            send_timer.AutoReset = true;
            send_timer.Enabled = true;
            is_routine_active = true;
            Console.WriteLine($"Routine started - sending XUID list every {interval_ms}ms");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error starting routine: {e.Message}");
        }
    }

    public static void ft_stop_routine()
    {
        if (send_timer != null)
        {
            send_timer.Stop();
            send_timer.Dispose();
            send_timer = null;
        }
        
        is_routine_active = false;
        Console.WriteLine("Routine stopped");
    }

    public static async void ft_send_xuid_list(object sender, ElapsedEventArgs e)
    {
        string[] xuid_array;
        string json_message;
        bool sent;

        try
        {
            if (!ServerGestion.ft_is_connected())
            {
                // Console.WriteLine("Not connected to server - skipping XUID send");
                return;
            }
            xuid_array = Utils.ft_get_xuid_array();
            json_message = Utils.ft_convert_xuid_to_json(xuid_array);
            if (string.IsNullOrEmpty(json_message))
            {
                // Console.WriteLine("Failed to convert XUIDs to JSON");
                return;
            }
            sent = await ServerGestion.ft_send_data(json_message);
            if (sent)
            {
                Console.WriteLine($"XUID list sent successfully");
                Utils.ft_clear_xuid_list();
            }
            else
            {
                Console.WriteLine("Failed to send XUID list");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending XUID list: {ex.Message}");
        }
    }
}