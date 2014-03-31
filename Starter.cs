using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace EGDL
{
    class Starter
    {
        public static void Main(String[] args)
        {
            String connectionString = @"Persist Security Info=False;User ID=pathcase;Password=dblab;Initial Catalog=polyposisdb_large3;Data Source=127.0.0.1;";
            Database db = new Database(connectionString);

            //open the database
            db.OpenConnection();
            if (args[0] == "label")
            {
                int PedigreeID = Convert.ToInt32(args[1]);
                EGDL egdl = new EGDL(db);
                DateTime start = DateTime.Now;
                egdl.EGDLEncoding(PedigreeID);
                TimeSpan time = DateTime.Now.Subtract(start);
                Console.WriteLine(time.TotalMilliseconds + " ms");
                Console.WriteLine("Finished!");
            }
            else if (args[0] == "average_inbreeding")
            {
                int PedigreeID = Convert.ToInt32(args[1]);
                InbreedingCalculation ic = new InbreedingCalculation(db, PedigreeID);
                double t = ic.AverageIC();
                Console.WriteLine("Inbreeding Coefficient: " + t);
            }
            else if (args[0] == "average_nc")
            {
                int PedigreeID = Convert.ToInt32(args[1]);
                InbreedingCalculation ic = new InbreedingCalculation(db, PedigreeID);
                double t = ic.AverageICNC();
                Console.WriteLine("Inbreeding Coefficient: " + t);
            }
            else if (args[0] == "test")
            {
                int PedigreeID = Convert.ToInt32(args[1]);
                InbreedingCalculation ic = new InbreedingCalculation(db, 0);
                Console.WriteLine(ic.CalculateNC(",1,1", "#,1#.0.0#,1,0#.0.0.0%.0.0.0%,1,1"));

            }
            //Console.Read();
            //close the database
            db.CloseConnection();
        }
    }
}
