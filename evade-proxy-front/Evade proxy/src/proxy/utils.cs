using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class Utils
{
    public static readonly string path_db = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "nufgy5-pivvun-womzaJ.db");
    public static FileStream f = null;
    public static ulong f_size = 0;
    private static List<string> xuid_list = new List<string>();
    private static object xuid_list_lock = new object();
    private static string server_name = "unknow";
    private static string server_ip = "unknow";
    private static string stamp_id = "unknow";
    public static List<(string gamertag, string color, string more)> gt_info = new List<(string, string, string)>();

    public static string ft_get_server_name()
    {
        return server_name;
    }

    public static void ft_set_server_name(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            server_name = name;
        }
    }

    public static string ft_get_server_ip()
    {
        return server_ip;
    }

    public static void ft_set_server_ip(string ip)
    {
        if (!string.IsNullOrEmpty(ip))
        {
            server_ip = ip;
        }
    }

    public static string ft_get_stamp_id()
    {
        return stamp_id;
    }

    public static void ft_set_stamp_id(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            stamp_id = id;
        }
    }

    public static void ft_add_xuid(string xuid)
    {
        if (!string.IsNullOrEmpty(xuid))
        {
            lock (xuid_list_lock)
            {
                if (!xuid_list.Contains(xuid))
                {
                    xuid_list.Add(xuid);
                    // Console.WriteLine($"XUID ajouté à la liste: {xuid}");
                }
            }
        }
    }

    public static List<string> ft_get_xuid_list()
    {
        lock (xuid_list_lock)
        {
            return new List<string>(xuid_list); // Retourne une copie
        }
    }

    public static void ft_clear_xuid_list()
    {
        lock (xuid_list_lock)
        {
            xuid_list.Clear();
            Console.WriteLine("Liste des XUID vidée");
        }
    }

    public static int ft_get_xuid_count()
    {
        lock (xuid_list_lock)
        {
            return xuid_list.Count;
        }
    }

    public static void ft_print_xuids()
    {
        lock (xuid_list_lock)
        {
            // Console.WriteLine($"Nombre total de XUID collectés: {xuid_list.Count}");
            foreach (string xuid_item in xuid_list)
            {
                Console.WriteLine($"XUID: {xuid_item}");
            }
        }
    }

    public static bool ft_remove_xuid(string xuid)
    {
        bool removed;

        if (!string.IsNullOrEmpty(xuid))
        {
            lock (xuid_list_lock)
            {
                removed = xuid_list.Remove(xuid);
                if (removed)
                {
                    Console.WriteLine($"XUID supprimé de la liste: {xuid}");
                }
                return removed;
            }
        }
        return false;
    }

    public static bool ft_contains_xuid(string xuid)
    {
        if (string.IsNullOrEmpty(xuid))
            return false;

        lock (xuid_list_lock)
        {
            return xuid_list.Contains(xuid);
        }
    }

    public static string[] ft_get_xuid_array()
    {
        lock (xuid_list_lock)
        {
            return xuid_list.ToArray();
        }
    }

    public static string ft_convert_xuid_to_json(string[] xuid_array)
    {
        List<long> xuid_numbers;
        long xuid_number;
        Dictionary<string, List<long>> message;
        string json_message;

        if (xuid_array.Length == 0)
        {
            // Console.WriteLine("No XUIDs to send");
            return null;
        }
        xuid_numbers = new List<long>();
        foreach (string xuid_str in xuid_array)
        {
            if (long.TryParse(xuid_str, out xuid_number))
            {
                xuid_numbers.Add(xuid_number);
            }
        }
        if (xuid_numbers.Count == 0)
        {
            Console.WriteLine("No valid XUIDs to send");
            return null;
        }
        message = new Dictionary<string, List<long>>
        {
            { "XUID", xuid_numbers }
        };

        json_message = JsonSerializer.Serialize(message);
        return json_message;
    }

    public static void ft_print_gt_info()
    {
        if (gt_info.Count == 0)
        {
            return;
        }
        Console.WriteLine("=== Player Information ===");
        foreach (var (gamertag, color, more) in gt_info)
        {
            Console.WriteLine($"Gamertag: {gamertag} | Color: {color} | More: {more}");
        }
        Console.WriteLine("==========================");
    }

    public static void ft_clear_gt_info()
    {
        gt_info.Clear();
        Console.WriteLine("Player info list cleared");
    }
}