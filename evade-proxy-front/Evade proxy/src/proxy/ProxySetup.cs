namespace Evade_proxy.src.proxy
{
    public class ProxySetup
    {
        static readonly string proxy_port = "8000";
        static ProxyManager proxyManager = new ProxyManager();

        // en attendant l'ui. a supprimer apres (non c bien pratique pour l'ui en soit) 
        static bool should_exit = false;
        static string last_server_name = "";
        static string last_server_ip = "";
        static string last_stamp_id = "";
        static int last_xuid_count = 0;
        static List<string> last_xuid_list = new List<string>();

        public static async Task ft_exit()
        {
            should_exit = true;
            Routine.ft_stop_routine();
            ServerGestion.ft_disconnect_from_server();
            ProxyManager.ft_disable_proxy_win_settings();
            await proxyManager.ft_stop_proxy();
            Utils.ft_clear_xuid_list();
            Console.WriteLine("Application cleanup completed");
        }

        static async Task ft_setup_server_communication()
        {
            bool connected;

            try
            {
                connected = await ServerGestion.ft_connect_to_server("XXX.XXX.XX.XX", XXXX);
                if (connected)
                {
                    _ = Task.Run(async () => await ServerGestion.ft_listen_for_messages());
                    Console.WriteLine("Server communication setup completed");
                    Routine.ft_start_routine(1 * 1000);
                }
                else
                {
                    throw new Exception("Failed to connect to server");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error setting up server communication: {exception.Message}");
                Environment.Exit(1);
            }
        }

        static async Task ft_start_proxy(string port)
        {
            try
            {
                await proxyManager.ft_setup_proxy();
                Console.WriteLine($"Proxy started on port {port}");
                ProxyManager.ft_enable_proxy_win_settings($"127.0.0.1:{port}");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error starting proxy: {exception.Message}");
                Environment.Exit(1);
            }
        }

        // en attendant l'ui. a supprimer apres (non c bien pratique pour l'ui en soit) 
        public static bool ft_has_data_changed()
        {
            string current_server_name = Utils.ft_get_server_name();
            string current_server_ip = Utils.ft_get_server_ip();
            string current_stamp_id = Utils.ft_get_stamp_id();
            int current_xuid_count = Utils.ft_get_xuid_count();
            var current_xuid_list = Utils.ft_get_xuid_list();
            bool has_changed = last_server_name != current_server_name ||
                              last_server_ip != current_server_ip ||
                              last_stamp_id != current_stamp_id ||
                              last_xuid_count != current_xuid_count ||
                              !last_xuid_list.SequenceEqual(current_xuid_list);
            if (has_changed)
            {
                last_server_name = current_server_name;
                last_server_ip = current_server_ip;
                last_stamp_id = current_stamp_id;
                last_xuid_count = current_xuid_count;
                last_xuid_list = new List<string>(current_xuid_list);
            }
            return has_changed;
        }
        public static async Task ft_setup_all()
        {
            await ft_start_proxy(proxy_port);
            await ft_setup_server_communication();
        }
    }
}