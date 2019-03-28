using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace SimpleAuth
{
    public class OAuthController
    {
        readonly WebAuthenticator authenticator;

        public OAuthController(WebAuthenticator authenticator)
        {
            this.authenticator = authenticator;
        }


        public async Task GetCredentials(string title, string details = "")
        {
            try
            {
                var url = await authenticator.GetInitialUrl();
                Console.WriteLine("******************");
                Console.WriteLine(title);
                Console.WriteLine(details);
                Console.WriteLine($"Launching Url: \"{url}\"");
                Console.WriteLine("******************");
                Console.WriteLine("Paste the Redirected URL Here:");
                OpenBrowser(url);
                var username = Console.ReadLine();

                try
                {
                    bool success = false;
                    var basic = authenticator;
                    if (basic != null)
                    {
                        success = basic.CheckUrl(new Uri(username), null);
                    }
                    if (!success)
                        throw new Exception("Invalid Credentials");
                }
                catch (Exception ex)
                {
                    await GetCredentials(title, $"Error: {ex.Message}");
                }
            }
            catch (TaskCanceledException)
            {
                authenticator.OnCancelled();
            }
        }

        public static void OpenBrowser(Uri uri)
        {
            OpenBrowser(uri.AbsoluteUri);
        }

        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
