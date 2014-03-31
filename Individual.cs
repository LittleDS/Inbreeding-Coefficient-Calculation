using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EGDL
{
    class Individual
    {
        public int PedigreeID;
        public int IndividualID;
        public int Gender;
        public int MotherID;
        public int FatherID;


        /// <summary>
        /// Constructur
        /// </summary>
        /// <param name="PID">PedigreeID</param>
        /// <param name="IID">IndividualID</param>
        /// <param name="G">Gender</param>
        /// <param name="MID">MotherID</param>
        /// <param name="FID">FatherID</param>
        public Individual(int PID, int IID, int G, int MID, int FID)
        {
            PedigreeID = PID;
            IndividualID = IID;
            Gender = G;
            MotherID = MID;
            FatherID = FID;
        }
    }
}
