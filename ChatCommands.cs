using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace AdminLogger
{

    [Torch.Commands.Category("AdminLogger")]
    public class ChatCommands : CommandModule
    {
        [Command("FixFaction", "seamless client switch")]
        [Permission(MyPromoteLevel.Admin)]

        public void FixFaction(long IdentityID)
        {
            MyFaction Fac = MySession.Static.Factions.GetPlayerFaction(IdentityID);
            Fac.KickMember(IdentityID);
            
            Context.Respond("Player Kicked From Faction!");
        }
    }
}
