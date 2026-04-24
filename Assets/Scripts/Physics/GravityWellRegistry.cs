using System.Collections.Generic;

namespace Vortex.Physics
{
    public static class GravityWellRegistry
    {
        private static readonly List<GravityWell> Wells = new List<GravityWell>();

        public static IReadOnlyList<GravityWell> GetAll()
        {
            return Wells;
        }

        public static void Register(GravityWell well)
        {
            if (well == null || Wells.Contains(well))
            {
                return;
            }

            Wells.Add(well);
        }

        public static void Unregister(GravityWell well)
        {
            if (well == null)
            {
                return;
            }

            Wells.Remove(well);
        }

        public static void FillData(List<GravityWellData> output)
        {
            if (output == null)
            {
                return;
            }

            output.Clear();
            for (int i = 0; i < Wells.Count; i++)
            {
                GravityWell well = Wells[i];
                if (well == null)
                {
                    continue;
                }

                output.Add(well.ToData());
            }
        }
    }
}
