using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace EGDL
{
    /// <summary>
    /// The basic class for representing a graph in memory
    /// </summary>
    class Graph
    {
        public Database db;

        /// <summary>
        /// The graph is stored in the database, so the 
        /// input parameter of the constructor is an instance of the database
        /// </summary>
        /// <param name="conn">A databse instance</param>
        public Graph(Database conn)
        {
            db = conn;
        }

        /// <summary>
        /// Get all the children of an individual, which is identified by both PedigreeID and IndividiaulID
        /// </summary>
        /// <param name="PedigreeID">The pedigree ID of that individual</param>
        /// <param name="IndividualID">The individual ID of that individual</param>
        /// <returns>Return a list of pairs of pedigree ID and individual ID</returns>
        public List<Individual> GetChildren(long PedigreeID, long IndividualID)
        {
            DataTable t;

            //The individual could be only mother or father, not both.
            //Therefore the following SQL is safe not to get redundant tuples
            db.ExecuteQuery(out t, "SELECT * FROM Individuals WHERE PedigreeID = {0} AND (MotherID = {1} OR FatherID = {1})",PedigreeID, IndividualID);
            if (t != null)
            {

                List<Individual> children = new List<Individual>();

                foreach (DataRow r in t.Rows)
                {
                    Individual i = new Individual((int)r["PedigreeID"], (int)r["IndividualID"], (int)r["Gender"], (int)r["MotherID"], (int)r["FatherID"]);
                    children.Add(i);
                }

                return children;
            }
            else 
                return null;
        }

        /// <summary>
        /// Get the TGDL code
        /// </summary>
        /// <param name="PedigreeID">The pedigree ID of that individual</param>
        /// <param name="IndividualID">The individual ID of that individual</param>
        /// <returns>Return the TGDL code of an individual</returns>
        public String GetTGDL(long PedigreeID, long IndividualID)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT TGDL FROM TGDL WHERE PedigreeID = {0} AND IndividualID = {1}", PedigreeID, IndividualID);
            if (t != null && t.Rows.Count > 0)
            {
                //The TGDL code of an individual is unique
                //so we can simply return it
                return (string)t.Rows[0]["TGDL"];
            }
            else
                return null;  //The individual doesn't have a TGDL code
        }

        public int GetID(String TGDL, int PedigreeID)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT IndividualID FROM TGDL WHERE PedigreeID = {0} AND TGDL = {1}", PedigreeID, TGDL);
            if (t != null && t.Rows.Count > 0)
            {
                //The TGDL code of an individual is unique
                //so we can simply return it
                return (int)t.Rows[0]["IndividualID"];
            }
            else
                return 0;  //The individual doesn't have a TGDL code
        }

        /// <summary>
        /// Set the TGDL code of one individual
        /// </summary>
        /// <param name="PedigreeID">The pedigree ID of that individual</param>
        /// <param name="IndividualID">The individual ID of that individual</param>
        /// <param name="TGDLCode"></param>
        public void SetTGDL(long PedigreeID, long IndividualID, String TGDLCode)
        {
            db.ExecuteNonQuery("INSERT INTO TGDL (PedigreeID, IndividualID, TGDL) VALUES ({0}, {1}, {2})", PedigreeID, IndividualID, TGDLCode);
        }

        /// <summary>
        /// Get the Non-tree Edges part of the EGDL encoding
        /// This method is just for convenience of encoding the pedigree graph
        /// </summary>
        /// <param name="PedigreeID">The pedigree ID of that individual</param>
        /// <param name="IndividualID">The individual ID of that individual</param>
        /// <returns></returns>
        public String GetNontreeEdges(long PedigreeID, long IndividualID)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT NontreeEdge FROM NontreeEdge WHERE PedigreeID = {0} AND IndividualID = {1}", PedigreeID, IndividualID);
            if (t != null && t.Rows.Count > 0)
            {
                return (string)t.Rows[0]["NontreeEdge"];
            }
            else
                return null;
        }

        /// <summary>
        /// Set the Non-tree edges part of the EGDL code
        /// </summary>
        /// <param name="PedigreeID">The pedigree ID of that individual</param>
        /// <param name="IndividualID">The individual ID of that individual</param>
        /// <param name="Code"></param>
        public void SetNontreeEdges(long PedigreeID, long IndividualID, String Code)
        {
            db.ExecuteNonQuery("INSERT INTO NontreeEdge (PedigreeID, IndividualID, NontreeEdge) VALUES ({0}, {1}, {2})", PedigreeID, IndividualID, Code);
        }
 
        /// <summary>
        /// If the Non-tree edges has already been created, then we should call this method to update it
        /// </summary>
        /// <param name="PedigreeID">The pedigree ID of that individual</param>
        /// <param name="IndividualID">The individual ID of that individual</param>
        /// <param name="Code"></param>
        public void UpdateNontreeEdges(long PedigreeID, long IndividualID, String Code)
        {
            db.ExecuteNonQuery("UPDATE NontreeEdge SET NontreeEdge = {0} WHERE PedigreeID = {1} AND IndividualID = {2}", Code, PedigreeID, IndividualID);
        }

        /// <summary>
        /// Get the list of different pedigree graph IDs
        /// </summary>
        /// <returns></returns>
        public List<int> NumberofPedigree()
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT distinct PedigreeID FROM Individuals ");

            if (t != null)
            {
                List<int> pedigreeIDs = new List<int>();
                foreach (DataRow r in t.Rows)
                {
                    pedigreeIDs.Add((int)r["PedigreeID"]);
                }

                return pedigreeIDs;
            }
            else
                return null;
        }


        /// <summary>
        /// Return all the individuals without parents for a specific pedigree graph
        /// </summary>
        /// <param name="PedigreeID">The pedigree ID</param>
        /// <returns>A list of indiviudals which don't have parents</returns>
        public List<Individual> IndividualsWOParents(long PedigreeID)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT * FROM Individuals WHERE PedigreeID = {0} AND MotherID = -1 AND FatherID = -1", PedigreeID);

            if (t != null)
            {
                List<Individual> indis = new List<Individual>();
                foreach (DataRow r in t.Rows)
                {
                    Individual i = new Individual((int)r["PedigreeID"], (int)r["IndividualID"], (int)r["Gender"], (int)r["MotherID"], (int)r["FatherID"]);
                    indis.Add(i);
                }

                return indis;
            }
            else
                return null;
        }

        /// <summary>
        /// Get the NontreeEdge code according to the given TGDL
        /// </summary>
        /// <param name="TGDL"></param>
        /// <param name="PedigreeID"></param>
        /// <returns></returns>
        public String TGDLtoNontreeEdge(String TGDL, int PedigreeID)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT e.NontreeEdge FROM TGDL t, NontreeEdge e WHERE t.PedigreeID = e.PedigreeID AND t.IndividualID = e.IndividualID AND t.PedigreeID = {0} AND t.TGDL = {1}", PedigreeID, TGDL);

            if (t != null && t.Rows.Count > 0)
            {
                return (string)t.Rows[0]["NontreeEdge"];
            }

            return null;
        }


        /// <summary>
        /// Get all the EGDL of one specific pedigree
        /// This method is to load all the EGDL to calculate the average inbreeding coefficient
        /// </summary>
        /// <param name="PedigreeID"></param>
        /// <returns></returns>
        public Dictionary<String, String> AllEGDL(int PedigreeID)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT t.TGDL, e.NontreeEdge FROM TGDL t, NontreeEdge e WHERE t.PedigreeID = e.PedigreeID AND t.IndividualID = e.IndividualID AND t.PedigreeID = {0}", PedigreeID);

            if (t != null)
            {
                Dictionary<String, String> egdls = new Dictionary<String, String>();
                foreach (DataRow r in t.Rows)
                {
                    String tgdl = (String)r["TGDL"];
                    String nontree = (String)r["NontreeEdge"];
                    egdls.Add(tgdl, nontree);
                }

                return egdls;
            }
            else
                return null;
        }

        /// <summary>
        /// Insert a tuple into the topology table
        /// </summary>
        /// <param name="PedigreeID"></param>
        /// <param name="OrderID"></param>
        /// <param name="TGDL"></param>
        public void InsertTopology(int PedigreeID, int OrderID, String TGDL)
        {
            db.ExecuteNonQuery("INSERT INTO Topology (PedigreeID, OrderID, TGDL) VALUES ({0}, {1}, {2})", PedigreeID, OrderID, TGDL);
        }


        /// <summary>
        /// Return the TGDLs in topological order of one specific pedigree
        /// </summary>
        /// <param name="PedigreeID"></param>
        /// <returns></returns>
        public List<String> GetTopology(int PedigreeID)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT TGDL FROM Topology WHERE PedigreeID = {0} ORDER BY OrderID ASC", PedigreeID);

            if (t != null)
            {
                List<String> tgdls = new List<String>();
                foreach (DataRow r in t.Rows)
                {
                    String tgdl = (String)r["TGDL"];
                    tgdls.Add(tgdl);
                }

                return tgdls;
            }
            else
                return null;
        }


        /// <summary>
        /// Set the out degiree of each individual
        /// </summary>
        /// <param name="PedigreeID"></param>
        /// <param name="IndividualID"></param>
        /// <param name="OutDegree"></param>
        public void SetOutDegree(int PedigreeID, String TGDL, int OutDegree)
        {
            db.ExecuteNonQuery("INSERT INTO OutDegree (PedigreeID, TGDL, OutDegree) VALUES ({0}, {1}, {2})", PedigreeID, TGDL, OutDegree);
        }

        /// <summary>
        /// Get the out degree of one specific individual
        /// </summary>
        /// <param name="PedigreeID"></param>
        /// <param name="IndividualID"></param>
        /// <returns></returns>
        public int GetOutDegree(int PedigreeID, String TGDL)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT OutDegree FROM OutDegree WHERE PedigreeID = {0} AND TGDL = {1}", PedigreeID, TGDL);

            if (t != null)
            {
                return (int)t.Rows[0]["OutDegree"];
            }
            else
                return -1;
        }


        /// <summary>
        /// Return all the out degree for one specific pedigree in the form of dictionary
        /// </summary>
        /// <param name="PedigreeID"></param>
        /// <returns></returns>
        public Dictionary<String, int> GetAllOutDegree(int PedigreeID)
        {
            DataTable t;
            db.ExecuteQuery(out t, "SELECT TGDL, OutDegree FROM OutDegree WHERE PedigreeID = {0}", PedigreeID);

            if (t != null)
            {
                Dictionary<String, int> degrees = new Dictionary<String, int>();
                foreach (DataRow r in t.Rows)
                {
                    String TGDL = (String)r["TGDL"];
                    int OutDegree = (int)r["OutDegree"];
                    degrees.Add(TGDL, OutDegree);
                }

                return degrees;
            }
            else
                return null;
        }
    }
}
