using FluentAssertions;
using MakingWaves.Tools.HyperVManagerHelper.PowerShellParsing;
using Xunit;

namespace MakingWaves.Tools.HyperVManagerHelper.Tests
{
    public class SharingParserTests
    {
        private readonly SharingParser _sharingParser = new SharingParser();

        [Fact]
        public void Ethernet_is_shared()
        {
            const string input = @"Name .......... : Ethernet
GUID .......... : {CC1ECB04-A772-4926-BF09-8CA164E8A22B}
Status ........ : Up
InterfaceType . : Ethernet
Unicast address : 1.1.1.1/255.255.255.0
Gateway ....... : 1.1.1.1
Device ........ : Intel(R) 82579LM Gigabit Network Connection
SharingType ... : ICSSHARINGTYPE_PUBLIC
";
            var sharingDto = _sharingParser.Parse(input);

            sharingDto.IsEthernetShared.Should().BeTrue();
        }

        [Fact]
        public void Ethernet_isnot_shared_and_wifi_is_shared()
        {
            const string input = @"Name .......... : Ethernet
GUID .......... : {CC1ECB04-A772-4926-BF09-8CA164E8A22B}
Status ........ : Up
InterfaceType . : Ethernet
Unicast address : 1.1.1.1/255.255.255.0
Gateway ....... : 1.1.1.1
Device ........ : Intel(R) 82579LM Gigabit Network Connection

Name .......... : Wi-Fi
GUID .......... : {CC1ECB04-A772-4926-BF09-8CA164E8A22B}
Status ........ : Down
InterfaceType . : Wireless80211
Device ........ : Intel(R) Centrino(R) Advanced-N 6205
SharingType ... : ICSSHARINGTYPE_PUBLIC
";
            var sharingDto = _sharingParser.Parse(input);

            sharingDto.IsEthernetShared.Should().BeFalse();
            sharingDto.IsWifiShared.Should().BeTrue();
        }
    }
}