using AlienRace;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnkindRaceDiversification.Patches
{
    public class ChjeeDroidFixes
    {
        //Transpiler method
        //  Lets droids spawn into other pawnkinds by preventing hostility response from being changed 
        public static IEnumerable<CodeInstruction> PawnHostilitySettingFix(IEnumerable<CodeInstruction> instructions)
        {
            int ifStatement = -1;
            int ifStatementStart = -1;
            int ifStatementEnd = -1;

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_0
                    && (codes[i + 1].operand != null
                        && codes[i + 1].operand.ToString().Contains("pawnBeingCrafted"))
                    && (codes[i + 2].operand != null
                        && codes[i + 2].operand.ToString().Contains("playerSettings"))
                    && codes[i + 3].opcode == OpCodes.Ldloc_S)
                {
                    ifStatementStart = i;
                    ifStatementEnd = i + 8;
                }
                if (codes[i].opcode == OpCodes.Brfalse_S
                    && codes[i + 1].opcode == OpCodes.Nop
                    && codes[i + 2].opcode == OpCodes.Ldloc_0
                    && (codes[i + 3].operand != null && codes[i + 3].opcode == OpCodes.Ldfld
                        && codes[i + 3].operand.ToString().Contains("pawnBeingCrafted"))
                    && codes[i + 4].opcode == OpCodes.Ldloc_S)
                {
                    ifStatement = i;
                }
            }
            if (ifStatementStart > -1
                && ifStatementEnd > -1
                && ifStatement > -1)
            {
                codes.Insert(ifStatementStart,
                    new CodeInstruction(codes[ifStatement]));
                codes.Insert(ifStatementStart,
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(PawnkindGenerationHijacker), "get_IsPawnOfPlayerFaction")));
            }

            /*
            string code = "Codes: \n";
            foreach (CodeInstruction c in codes)
            {
                code += c.opcode.ToString();
                if (c.operand != null)
                    code += c.operand;
                code += "\n";
            }
            PawnkindRaceDiversification.Logger.Message(code);
            */

            return codes.AsEnumerable();
        }
    }
}
