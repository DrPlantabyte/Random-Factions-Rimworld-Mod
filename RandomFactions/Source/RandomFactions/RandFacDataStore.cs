

using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RandomFactions
{
    public class RandFacDataStore : WorldComponent
    {
        private List<RimWorld.FactionDef> worldFactionDefs = new List<RimWorld.FactionDef>();
        private List<RimWorld.XenotypeDef> worldXenotypeDefs = new List<RimWorld.XenotypeDef>();
        private bool initialized = false;
        private bool synchronized = false; // <-- do not store this value!
        public int xenotypePercent = 0;
        private System.Random prng;
        public HugsLib.Utils.ModLogger Logger = null;
        public RandFacDataStore(World world) : base(world)
        {
            /*
             Constructor called every time this game world begins
             */
            int seed = iterateSeed(iterateSeed(world.ConstantRandSeed));
            this.prng = new System.Random(seed);
            this.Logger = new HugsLib.Utils.ModLogger("RandomFactions.RandFacDataStore"); // should be overwriten
        }

        private int iterateSeed(int seed)
        {
            System.Random seeder = new System.Random(seed);
            byte[] seed_buffer = new byte[4];
            seeder.NextBytes(seed_buffer);
            return BitConverter.ToInt32(seed_buffer, 0);
        }

        public List<RimWorld.FactionDef> getFactionDefs()
        {
            synchronizeFactionDefs();
            // TODO
            return this.worldFactionDefs;
        }

        public void synchronizeFactionDefs()
        {
            if (!this.initialized)
            {
                initializeFactionDefs();
            }
            if (!this.synchronized)
            {
                // add any procedural factions back into the game
                this.synchronized = true;
                var existingFactionDefNames = new HashSet<string>();
                foreach(var def in DefDatabase<FactionDef>.AllDefs)
                {
                    existingFactionDefNames.Add(def.defName);
                }
                foreach(var def in this.worldFactionDefs)
                {
                    if (!existingFactionDefNames.Contains(def.defName))
                    {
                        // see https://ludeon.com/forums/index.php?topic=33983.0 
                        DefDatabase<FactionDef>.Add(def);
                    }
                }
                Logger.Trace(string.Format("Synchronized faction defs. \nWorld factions: {0}\n\nSystem-wide factions: {1}",
                    defListToString(this.worldFactionDefs), this.defListToString(DefDatabase<FactionDef>.AllDefs)));
            }
        }

        private void initializeFactionDefs()
        {
            this.initialized = true;

            Logger.Trace(string.Format("Found {0} faction definitions: {1}", DefDatabase<FactionDef>.DefCount, 
                defListToString(DefDatabase<FactionDef>.AllDefs)));
           foreach(var def in DefDatabase<FactionDef>.AllDefs)
            {
                if (def.categoryTag.EqualsIgnoreCase(RandomFactionsMod.RANDOM_CATEGORY_NAME)) { continue; } // skip factions from this mod
                if (def.isPlayer) { continue; } // skip player factions
                worldFactionDefs.Add(def);
            }
            Logger.Trace(string.Format("Found {0} xenotype definitions: {1}", DefDatabase<XenotypeDef>.DefCount, defListToString(DefDatabase<XenotypeDef>.AllDefs)));
            Logger.Trace(string.Format("Baseliner to Xenotype conversion rate set to {0}%", this.xenotypePercent));
            foreach (var def in DefDatabase<XenotypeDef>.AllDefs)
            {
                worldXenotypeDefs.Add(def);
            }
        }

        public FactionDef patchXenotype(FactionDef def)
        {
            if (def.isPlayer) { return def; } // skip player factions
            //Logger.Trace(string.Format("Patching random xenotype into faction def {0}...", def.defName));
            // patch-in a random xenotype
            var def2 = copyDef(def); // need to do a 1-level deep copy to avoid messing with the original
            var xeno = this.worldXenotypeDefs[this.prng.Next(this.worldXenotypeDefs.Count)];
            Logger.Trace(string.Format("Creating {0} xenotype version of {1} faction ({2})...", xeno.defName, def.defName, XenotypeSetToString(def.xenotypeSet)));
            def2.defName = xeno.defName + def.defName;
            var xenoChance = new XenotypeChance(xeno, 1.0f);
            List<XenotypeChance> xenotypeChances = new List<XenotypeChance>
            {
                xenoChance
            };
            var newXenoSet = new XenotypeSet();
            // I think Ludeon Studios hates procedural generation. Why make XenotypeSet read-only with no constructor?!
            // Need to use reflection voodoo to modify private variable (whose name might change in a future version)
            FieldInfo[] fields = typeof(XenotypeSet).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.IsAssignableFrom(xenotypeChances.GetType()))
                {
                    field.SetValue(newXenoSet, xenotypeChances);
                }
            }
            def2.xenotypeSet = newXenoSet;
            Logger.Trace(string.Format("New world-local faction def {0} has xenotype set: {1}", def2.defName, XenotypeSetToString(def2.xenotypeSet)));
            // finally!
            worldFactionDefs.Add(def2);
            DefDatabase<FactionDef>.Add(def2);
            return def2;
        }

        private string defListToString(IEnumerable<Def> allDefs)
        {
            string s = "";
            foreach (var def in allDefs)
            {
                if (s.Length > 0) { s += ", "; }
                s += def.defName;
            }
            return s;
        }

        private FactionDef copyDef(FactionDef def)
        {
            FactionDef cpy = new FactionDef();
            reflectionCopy(def, cpy);
            cpy.debugRandomId = (ushort)(def.debugRandomId + 1);
            return cpy;
        }

        private void reflectionCopy(object A, object B)
        {
            FieldInfo[] fields = A.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                var value = field.GetValue(A);
                field.SetValue(B, value);
            }
            /*
            PropertyInfo[] props = A.GetType().GetProperties();
            foreach(PropertyInfo prop in props)
            {
                var value = prop.GetValue(A, null);
                prop.SetValue(B, value, null);
            }*/
        }

        private string XenotypeSetToString(XenotypeSet xs)
        {
            if (xs == null) return "N/A";
            string s = "";
            for(int n = 0; n < xs.Count; n++)
            {
                var x = xs[n];
                if (s.Length > 0) { s += ", "; }
                s += ((int)(x.chance * 100))+"% "+x.xenotype.defName;
            }
            return s;
        }

        public override void ExposeData()
        {
            /*
            This method is called to synchronize the data
            */
            base.ExposeData();
            Scribe_Values.Look(ref initialized, "initialized");
            // do not store this.synchronized!
            Scribe_Values.Look(ref xenotypePercent, "xenotypePercent");
            Scribe_Collections.Look(ref worldFactionDefs, "worldFactionDefs");
            Scribe_Collections.Look(ref worldXenotypeDefs, "worldXenotypeDefs");
        }

        private void ExposeFactionDefListData(ref List<FactionDef> defList, string keyName) { 
            int count = defList.Count;
            Scribe_Values.Look(ref count, keyName+"_size");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                for(int i = 0; i < count; i++)
                {
                    var def = defList[i];
                    // TODO: manually save and loaf def data tree using reflection
                    Scribe
                    Scribe_Defs.Look(ref def, keyName + "_"+i);
                }
            }
    }
}
