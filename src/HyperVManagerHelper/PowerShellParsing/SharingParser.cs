using System;
using System.Text.RegularExpressions;

namespace MakingWaves.Tools.HyperVManagerHelper.PowerShellParsing
{
    public class SharingParser
    {
        public SharingDto Parse(string input)
        {
            SharingDto sharingDto = new SharingDto();

            var lines = input.Split(new[] {Environment.NewLine}, StringSplitOptions.None);

            string currentConnectionName = null;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Regex regex = new Regex(@"Name \.\.\.\.\.\.\.\.\.\. : ([\w-]+)");
                var match = regex.Match(line);
                if (match.Success)
                {
                    currentConnectionName = match.Groups[1].Value;
                }

                if (line == "")
                {
                    currentConnectionName = null;
                }

                if (line == "SharingType ... : ICSSHARINGTYPE_PUBLIC")
                {
                    switch (currentConnectionName)
                    {
                        case "Ethernet":
                            sharingDto.IsEthernetShared = true;
                            break;
                        case "Wi-Fi":
                            sharingDto.IsWifiShared = true;
                            break;
                    }
                }
            }

            return sharingDto;
        }
    }
}