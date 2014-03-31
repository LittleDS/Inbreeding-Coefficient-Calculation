using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EGDL
{
    class Pair
    {
        public String nodeCode;
        public int pathLength;
        public Pair(String a, int b)
        {
            nodeCode = a;
            pathLength = b;
        }
    }
    /// <summary>
    /// The class of methods for calculating inbreeding coefficient
    /// </summary>
    class InbreedingCalculation
    {
        private Dictionary<String, Dictionary<String, double>> storedResults = new Dictionary<String, Dictionary<String, double>>();
        
        private Dictionary<String, List<String>> parentChildren = new Dictionary<string, List<string>>();

        ////time measurer
        //double totalTimeForPath = 0;
        //double totalTimeForParents = 0;
        //double totalTimeForOverlapping = 0;

        ////space measure
        //double totalLengthCPE = 0;
        //double totalLengthNodeCode = 0;

        // Used for identifying mother and father
        Graph localGraph;
        int currentPedigree;

        //For calculating the average inbreeding coefficient
        //We have to load all the egdls into the memory
        Dictionary<String, String> egdls;
        
        //This table will store the inbreeding coefficient which has been already calculated
        Dictionary<String, double> ICTable = new Dictionary<string, double>();

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="individualEGDL">The EGDL code of an individual</param>
        /// <param name="db">The database connection</param>
        public InbreedingCalculation(Database db, int PedigreeID)
        {
            localGraph = new Graph(db);
            currentPedigree = PedigreeID;
            egdls = null;
        }


        /// <summary>
        /// Calculate the average inbreeding coefficient of all the individuals
        /// </summary>
        /// <returns></returns>
        public double AverageIC()
        {       
            double total = 0.0;
            //The key is the TGDL
            //The value is the NontreeEdge
            egdls = localGraph.AllEGDL(currentPedigree);
            DateTime start = DateTime.Now;
            //Calculate the average inbreeding coefficient using the 
            foreach (String k in egdls.Keys)
            {
                //First check whether this individual has been calculated or not
                if (ICTable.ContainsKey(k))
                    total += ICTable[k];
                else
                {
                    double tic = Calculate(k, egdls[k]);
                    //if (tic != 0)
                    //    Console.WriteLine(localGraph.GetID(k,currentPedigree) + " " + tic);
                    total += tic;
                    ICTable.Add(k, tic);
                }
               // parentChildren.Clear();
            }

            TimeSpan time = DateTime.Now.Subtract(start);
            Console.WriteLine("Total Time:" + time.TotalMilliseconds + " ms");
            //Console.WriteLine("Time For Path Construction: " + totalTimeForPath + " ms");
            //Console.WriteLine("The Total Length for CPE " + totalLengthCPE);
            return total / egdls.Count;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double AverageICNC()
        {
            DateTime start = DateTime.Now;
            double total = 0.0;

            //The key is the TGDL
            //The value is the NontreeEdge
            egdls = localGraph.AllEGDL(currentPedigree);

            //Calculate the average inbreeding coefficient using the 
            foreach (String k in egdls.Keys)
            {
                //First check whether this individual has been calculated or not
                if (ICTable.ContainsKey(k))
                    total += ICTable[k];
                else
                {
                    double tic = CalculateNC(k, egdls[k]);
                    //if (tic != 0)
                    //    Console.WriteLine(localGraph.GetID(k,currentPedigree) + " " + tic);
                    total += tic;
                    ICTable.Add(k, tic);
                }
                //Console.WriteLine(ICTable[k]);
            }

            TimeSpan time = DateTime.Now.Subtract(start);
            Console.WriteLine("Total Time:" + time.TotalMilliseconds + " ms");
            //Console.WriteLine("Time for Identifying Common Ancestors " + totalTimeForParents + " ms");
            //Console.WriteLine("Time For Path Construction " + totalTimeForPath + " ms");
            //Console.WriteLine("Time For Overlapping Checking " + totalTimeForOverlapping + " ms");
            //Console.WriteLine("Total Length for NodeCode " + totalLengthNodeCode);
            return total / egdls.Count;
        }
        /// <summary>
        /// Calculate the inbreeding coefficient for one specific individual given the IndividualID
        /// </summary>
        /// <param name="IndividualID"></param>
        /// <returns></returns>
        public double Calculate(int IndividualID)
        {
            String TGDL = localGraph.GetTGDL(currentPedigree, IndividualID);
            String NontreeEdge = localGraph.GetNontreeEdges(currentPedigree, IndividualID);
            //Console.WriteLine(TGDL + " " + NontreeEdge);
            return Calculate(TGDL, NontreeEdge);
        }

        /// <summary>
        /// Calculate the inbreeding coefficient for a specific individual
        /// </summary>
        /// <returns>Inbreeding Coefficient</returns>
        public double Calculate(String TGDL, String NontreeEdge)
        {
            //The structure storing the paths already found
            //The key is the TGDL of the individual, the value is the set of paths from this individual to the target individual
            Dictionary<String, List<List<String>>> mpaths = new Dictionary<String, List<List<String>>>();
            Dictionary<String, List<List<String>>> fpaths = new Dictionary<String, List<List<String>>>();

            //If the inbreeding coefficient is existed, just return it
            if (ICTable.ContainsKey(TGDL))
                return ICTable[TGDL];

            double result = 0.0;
           
            //Fetch TGDL code from EGDL code
            String[] temp = NontreeEdge.Split(Utils.Delimiters);

            String[,] NontreeEdges = new String[(temp.Length - 1) / 2, 2];

            //Assign the non-tree edges
            for (int i = 1; i < temp.Length; i += 2)
            {
                NontreeEdges[(i - 1) / 2, 0] = temp[i];
                NontreeEdges[(i - 1) / 2, 1] = temp[i + 1];
            }

            //First identify mother and father, and the TGDL of them will be stored in the global variable
            //MotherTGDL and FatherTGDL
            String[] tempParents = IdentifyMF(TGDL, NontreeEdges);
            
            String FatherTGDL = "";
            String MotherTGDL = "";
            if (tempParents != null)
            {
                FatherTGDL = tempParents[0];
                if (!parentChildren.ContainsKey(FatherTGDL))
                    parentChildren.Add(FatherTGDL, new List<string>());
                    parentChildren[FatherTGDL].Add(TGDL);

                MotherTGDL = tempParents[1];
                if (!parentChildren.ContainsKey(MotherTGDL))
                    parentChildren.Add(MotherTGDL, new List<String>());
                parentChildren[MotherTGDL].Add(TGDL);
            }

            if (FatherTGDL == "" || MotherTGDL == "")
                return 0.0;


            //the siblings have exactly the same inbreeding coefficient
            if (storedResults.ContainsKey(FatherTGDL) && storedResults[FatherTGDL].ContainsKey(MotherTGDL))
                return storedResults[FatherTGDL][MotherTGDL];

            String FatherNontreeEdge = "";
            String MotherNontreeEdge = "";


            //Get the NontreeEdge of parents
            if (FatherTGDL != "")
                FatherNontreeEdge = Utils.GetNontreeEdgeFromTGDL(FatherTGDL, NontreeEdge, 1);

            if (MotherTGDL != "")
                MotherNontreeEdge = Utils.GetNontreeEdgeFromTGDL(MotherTGDL, NontreeEdge, 2);



            List<String> ancestors = IdentifyCommonAncestor(Utils.Delimiter + MotherTGDL + MotherNontreeEdge, Utils.Delimiter + FatherTGDL + FatherNontreeEdge);

            //s reprents the TGDL code of ancestor
            foreach (String s in ancestors)
            {
                //First check whether the inbreeding coefficient of this ancestor has been calculated
                //if not, calculate it first
                if (!ICTable.ContainsKey(s))
                {
                    double tempIC;
                    if (egdls != null && egdls.ContainsKey(s))
                        tempIC = Calculate(s, egdls[s]);
                    else //we have to access to database to get the EGDL of this ancestor here
                        tempIC = Calculate(s, localGraph.TGDLtoNontreeEdge(s,currentPedigree));

                    //store the calculated value in the table
                    ICTable[s] = tempIC;
                }

                //until this point, the inbreeding coefficient must be in the table
                double Fa = ICTable[s];

               // DateTime start = DateTime.Now;
                //Get the paths to mother
                FirstTime = true;
                List<List<String>> motherPaths = PathFinder(MotherTGDL, MotherNontreeEdge, s, 1, mpaths, fpaths);

                //Get the paths to father
                FirstTime = true;
                List<List<String>> fatherPaths = PathFinder(FatherTGDL, FatherNontreeEdge, s, 2, mpaths, fpaths);
                
                //TimeSpan time = DateTime.Now.Subtract(start);               
                //totalTimeForPath += time.TotalMilliseconds;
    

                if (motherPaths.Count == 0 && fatherPaths.Count != 0)
                {
                    for (int i = 0; i < fatherPaths.Count; i++)
                        result += Math.Pow(0.5, fatherPaths[i].Count) * (1 + Fa);
                }
                else if (fatherPaths.Count == 0 && motherPaths.Count != 0)
                {
                    for (int i = 0; i < motherPaths.Count; i++)
                        result += Math.Pow(0.5, motherPaths[i].Count) * (1 + Fa);
                }
                else 
                {
                    bool executeFlag = true;
                    //if (parentChildren.ContainsKey(s))
                    //{
                        if (parentChildren.ContainsKey(s) && parentChildren[s].Count == 1)
                            executeFlag = false;

                        //foreach (String child in parentChildren[s])
                        //{
                        //    if (child != MotherTGDL && child != FatherTGDL)
                        //        if (mpaths.ContainsKey(child) && mpaths[child].Count == motherPaths.Count &&
                        //            fpaths.ContainsKey(child) && fpaths[child].Count == fatherPaths.Count)
                        //        {
                        //            executeFlag = false;
                        //            break;
                        //        }
                        //}
                   // }
                    if (executeFlag)
                    {
                        bool[,] tM = EliminateOverlapping(motherPaths, fatherPaths);
                        int N1 = motherPaths.Count;
                        int N2 = fatherPaths.Count;

                        for (int i = 0; i < N1; i++)
                            for (int j = 0; j < N2; j++)
                            {
                                //if they are non-overlapping
                                if (tM[i, j])
                                {
                                    result += Math.Pow(0.5, motherPaths[i].Count + fatherPaths[j].Count - 1) * (1 + Fa);

                                    //Print the pairs of non-overlapping paths for debugging purpose
                                    //Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~");
                                    //OutputPath(motherPaths[i], fatherPaths[j]);
                                    //Console.WriteLine("-------------------------"); 
                                }
                            }
                    }
                    else
                        Console.WriteLine("For " + TGDL + " Successfully Ignore " + s);
                }
            }

            if (!storedResults.ContainsKey(FatherTGDL))
                storedResults.Add(FatherTGDL, new Dictionary<String, double>());
            
            // store results in memory for future use
            if (!storedResults[FatherTGDL].ContainsKey(MotherTGDL))
                storedResults[FatherTGDL].Add(MotherTGDL, result);

            return result;
        }


        /// <summary>
        /// Identify Mother and Father using only the EGDL code
        /// </summary>
        public String[] IdentifyMF(String TGDLCode, String[,] NontreeEdges)
        {
            String FatherTGDL = "";
            String MotherTGDL = "";

            //From the TGDL code of the individual, we can identify one of its parent
            int i = TGDLCode.Length - 1;
            while (i >= 0 && TGDLCode[i] != Utils.Female && TGDLCode[i] != Utils.Male)
                i--;
            String oneparent = TGDLCode.Substring(0, i);
            
            //it means that there is no parent
            if (oneparent.Length == 0)
                return null;

            //Get the gender of this parent
            int j = oneparent.Length - 1;
            while (j>= 0 && oneparent[j] != Utils.Female && oneparent[j] != Utils.Male)
                j--;
            //According to the Gender of the parent, assign it to Mother or Father
            if (oneparent[j] == Utils.Male)
                FatherTGDL = oneparent;
            else
                MotherTGDL = oneparent;

            //Get the other parent from the non-tree edges part if possible
            for (int k = 0; k < NontreeEdges.Length / 2; k++)
            {
                if (NontreeEdges[k, 1] == TGDLCode)
                {
                    if (FatherTGDL == "")
                        FatherTGDL = NontreeEdges[k, 0];
                    else if (MotherTGDL == "")
                        MotherTGDL = NontreeEdges[k, 0];
                }
            }

            String[] r = new String[2];
            r[0] = FatherTGDL;
            r[1] = MotherTGDL;

            return r;
        }

        /// <summary>
        /// Identify the common ancestors of two individuals
        /// </summary>
        /// <param name="EGDLa">The EGDL code of the first individual</param>
        /// <param name="EGDLb">The EGDL code of the second individual</param>
        /// <returns>A List of TGDL code representing the common ancestors</returns>
        public List<String> IdentifyCommonAncestor(String EGDLa, String EGDLb)
        {
            //Get the unique prefix set of first EGDL
            HashSet<String> Prefixa = new HashSet<String>();
            if (EGDLa != null)
            {
                String[] temp = EGDLa.Split(Utils.Delimiters);
                for (int i = 1; i < temp.Length; i++)
                {
                    String t = temp[i];

                    //Get the prefix
                    int j = 1;
                    while (j < t.Length) {

                        //Get the prefixes piece by piece
                        while (j < t.Length && t[j] != Utils.Female && t[j] != Utils.Male)
                            j++;
                        String piece = t.Substring(0, j);
                        if (!Prefixa.Contains(piece))
                            Prefixa.Add(piece);
                        j++;
                    }
                }
            }


            //Get the unique prefix set of second EGDL
            HashSet<String> Prefixb = new HashSet<String>();
            if (EGDLb != null)
            {
                String[] temp = EGDLb.Split(Utils.Delimiters);
                for (int i = 1; i < temp.Length; i++)
                {
                    String t = temp[i];
                    
                    //Get the prefix
                    int j = 1;
                    while (j < t.Length)
                    {
                        //Get the prefixes piece by piece
                        while (j < t.Length && t[j] != Utils.Female && t[j] != Utils.Male)
                            j++;
                        String piece = t.Substring(0, j);
                        if (!Prefixb.Contains(piece))
                            Prefixb.Add(piece);
                        j++;
                    }
                }
            }

            Prefixa.IntersectWith(Prefixb);

            List<String> r = new List<String>();
            //Get the intersection of these two sets
            foreach (String s in Prefixa)
            {
                r.Add(s);
            }
             
            return r;
        }


        bool FirstTime = true;
        String[] temp;
        String[,] tempNontreeEdges;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TGDL">The TGDL of the parent</param>
        /// <param name="NontreeEdge">The Nontree Part of the parent</param>
        /// <param name="AncestorTGDL">The TGDL of the ancestor</param>
        /// <param name="Gender">The Gender of the parents</param>
        /// <returns></returns>
        public List<List<String>> PathFinder(String TGDL, String NontreeEdge, String AncestorTGDL, int Gender, Dictionary<String, List<List<String>>> mpaths, Dictionary<String, List<List<String>>> fpaths)
        {
            if (Gender == 1 && fpaths.ContainsKey(AncestorTGDL))
            {
                return fpaths[AncestorTGDL];
            }
            else if (Gender == 2 && mpaths.ContainsKey(AncestorTGDL))
            {
                return mpaths[AncestorTGDL];
            }

            //1.	Result Set = {}
            List<List<String>> r = new List<List<String>>();

            if (FirstTime)
            {
                temp = NontreeEdge.Split(Utils.Delimiters);

                //Record the non-tree edges part of EGDL code
                tempNontreeEdges = new String[(temp.Length - 1) / 2, 2];

                //Assign the non-tree edges
                for (int i = 1; i < temp.Length; i += 2)
                {
                    tempNontreeEdges[(i - 1) / 2, 0] = temp[i];
                    tempNontreeEdges[(i - 1) / 2, 1] = temp[i + 1];
                }

                FirstTime = false;
            }

            if (TGDL == AncestorTGDL)
                return r;

            //3.  If t is a prefix of t’
            if (Utils.IsPrefix(AncestorTGDL, TGDL))
            {
                //a)  Extend a path using t and t’
                List<String> tempPath = Utils.ExtendPath(AncestorTGDL, TGDL);

                //b)  Add the path to the Result Set
                r.Add(tempPath);
            }

            // 4.	For all the pairs of nodes (vsource, vdesination)
            for (int i = 0; i < tempNontreeEdges.Length / 2; i++)
            {
                //a)	If t is a prefix of vsource
                if (Utils.IsPrefix(AncestorTGDL, tempNontreeEdges[i, 0]))
                {
                    List<List<String>> tempResult = new List<List<String>>();

                    //i.	if (vdestination has been calculated)
                    if (Gender == 1 && fpaths.ContainsKey(tempNontreeEdges[i, 1]))
                    {
                        //Fetch the pre-calculated result from the table
                        tempResult = fpaths[tempNontreeEdges[i, 1]];
                    }
                    else if (Gender == 2 && mpaths.ContainsKey(tempNontreeEdges[i, 1]))
                    {
                        tempResult = mpaths[tempNontreeEdges[i, 1]];
                    }


                    //ii.	else R = FindPath(e, vdestination)
                    else
                    {
                        //If the set of paths is not pre-calculated,
                        //then we have to recursively calculate it

                        tempResult = PathFinder(TGDL, NontreeEdge, tempNontreeEdges[i, 1], Gender, mpaths, fpaths);
                    }

                    //iii.	Extend a path pa using t and vsource
                    List<String> formerPath = Utils.ExtendPath(AncestorTGDL, tempNontreeEdges[i, 0]);

                    //Contact pa and (vsource, vdestination)
                    formerPath.Add(tempNontreeEdges[i, 1]);

                    //iv.	Contact pa and (vsource, vdestination) to every path in R in the front
                    List<List<String>> tempReturn = Utils.ConnectPaths(formerPath, tempResult);

                    //v.	Add the above paths to Result Set
                    if (tempReturn != null && tempReturn.Count > 0)
                        r.AddRange(tempReturn);
                }
            }

            //Add the calculated result to the Dictionary paths
            if (Gender == 1 && !fpaths.ContainsKey(AncestorTGDL))
            {
                fpaths.Add(AncestorTGDL, r);
            }
            else if (Gender == 2 && !mpaths.ContainsKey(AncestorTGDL))
            {
                mpaths.Add(AncestorTGDL, r);
            }

            //5.	Return Result Set
            return r;
        }

        public bool[,] EliminateOverlapping(List<List<String>> PathSet1, List<List<String>> PathSet2)
        {
            //The inverted index
            //The key is TGDL code appearing in the paths
            //The value is the index of the path
            Dictionary<String, List<int>> invertedIndex = new Dictionary<String, List<int>>();

            //Build the inverted index first
            int N1 = PathSet1.Count;
            int N2 = PathSet2.Count;
            for (int i = 0; i < N1; i++)
            {
                List<String> temp = PathSet1[i];
                // The first node and the last node will always be the same
                // Since all the source and destination of these paths are the same
                // We can simply ignore these two points
                for (int j = 1; j < temp.Count - 1; j++)
                {
                    if (!invertedIndex.ContainsKey(temp[j]))
                    {
                        List<int> tList = new List<int>();
                        tList.Add(i);
                        invertedIndex.Add(temp[j], tList);
                    }
                    else
                    {
                        invertedIndex[temp[j]].Add(i);
                    }

                }
            }

            for (int i = 0; i < N2; i++)
            {
                List<String> temp = PathSet2[i];
                for (int j = 1; j < temp.Count - 1; j++)
                {
                    if (!invertedIndex.ContainsKey(temp[j]))
                    {
                        List<int> tList = new List<int>();
                        tList.Add(i + N1);  //Make the difference between the PathSet1 and PathSet2
                        invertedIndex.Add(temp[j], tList);
                    }
                    else
                    {
                        invertedIndex[temp[j]].Add(i + N1);
                    }
                }
            }

            //Using the matrix to eliminate the overlapping ones
            bool[,] M = new bool[N1, N2];
            for (int i = 0; i < N1; i++)
                for (int j = 0; j < N2; j++)
                    M[i, j] = true;

            foreach (String s in invertedIndex.Keys)
            {
                List<int> tp = invertedIndex[s];
                for (int i = 0; i < tp.Count; i++)
                {
                    int x = tp[i];
                    if (tp[i] >= N1)
                        continue;
                    for (int j = i + 1; j < tp.Count; j++)
                    {
                        if (tp[j] < N1)
                            continue;
                        int y = tp[j] - N1;
                        M[x, y] = false;
                    }
                }
            }

            return M;
        }
        public void OutputPath(List<String> P1, List<String> P2)
        {
            int l = P1.Count;

            for (int i = 0; i < l; i++)
            {
                if (i != l - 1)
                    Console.Write(P1[i] + "->");
                else
                    Console.WriteLine(P1[i]);
            }

            int k = P2.Count;

            for (int i = 0; i < k; i++)
            {
                if (i != k - 1)
                    Console.Write(P2[i] + "->");
                else
                    Console.WriteLine(P2[i]);
            }
        }
        
        /// <summary>
        /// Convert CPE to NodeCode
        /// </summary>
        /// <param name="TGDL"></param>
        /// <param name="NontreeEdge"></param>
        public Dictionary<String, List<String>> CPEtoSubGraph(String TGDL, String NontreeEdge) 
        {
            String[] edgeSet = NontreeEdge.Split(Utils.Delimiters);

            //Record the non-tree edges part of EGDL code
            String[,] NontreeEdges = new String[(edgeSet.Length - 1) / 2, 2];

            //Assign the non-tree edges
            for (int i = 1; i < edgeSet.Length; i += 2)
            {
                NontreeEdges[(i - 1) / 2, 0] = edgeSet[i];
                NontreeEdges[(i - 1) / 2, 1] = edgeSet[i + 1];
            }

            Queue<String> Q = new Queue<string>();
            Q.Enqueue(TGDL);
            
            //The adjacency list of the subgraph
            Dictionary<String, List<String>> T = new Dictionary<String, List<String>>();
            
            //Store the PET code as the ID of each progenitor
            List<String> progenitors = new List<String>();
            
            Dictionary<String, int> parentsLeft = new Dictionary<String, int>();

            while (Q.Count > 0)
            {
                String P = Q.Dequeue();
                
                //The tree parent
                int p = P.LastIndexOfAny(Utils.Genders);
                String OneParent = P.Substring(0, p);
                if (OneParent.Length > 0)
                {
                    if (!T.ContainsKey(OneParent))
                        T.Add(OneParent, new List<String>());
                    if (!T[OneParent].Contains(P))
                    {
                        T[OneParent].Add(P);
                        Q.Enqueue(OneParent);
                    }

                    if (!parentsLeft.ContainsKey(P))
                        parentsLeft.Add(P, 1);

                    //The non-tree parent
                    String TwoParent = null;
                    for (int i = 0; i < NontreeEdges.Length / 2; i++)
                    {
                        //compare P with Vd
                        if (NontreeEdges[i, 1] == P)
                        {
                            TwoParent = NontreeEdges[i, 0];
                            break;
                        }
                    }

                    //if we find the non-tree parent
                    if (TwoParent != null)
                    {
                        if (!T.ContainsKey(TwoParent))
                            T.Add(TwoParent, new List<String>());
                        if (!T[TwoParent].Contains(P))
                        {
                            T[TwoParent].Add(P);
                            Q.Enqueue(TwoParent);
                        }


                        //This individual has two parents
                        parentsLeft[P] = 2;
                    }

                }
                else  //we found a progenitor
                {
                    if (!progenitors.Contains(P))
                        progenitors.Add(P);
                    if (!parentsLeft.ContainsKey(P))
                        parentsLeft.Add(P, 0);
                }

            }

            //we have got the subgraph, start encoding NodeCode

            //The key is the PET code of one individual, and the PET code is used as the ID for that individual
            //The value is the list of NodeCodes for that individual
            Dictionary<String, List<String>> result = new Dictionary<string,List<string>>();
            
            //We use the BFS to do the NodeCode encoding
            Queue<String> N = new Queue<string>();

            int countID = 0;

            Dictionary<String, char> statusOfGraph = new Dictionary<String, char>();

            foreach (String s in progenitors)
            {
                N.Enqueue(s);
                
                statusOfGraph.Add(s, 'G');
                
                //Get the gender of the individual
                int gPos = s.LastIndexOfAny(Utils.Genders);

                List<String> tempList = new List<String>();

                if (s[gPos] == Utils.Female)
                    tempList.Add(countID + "" + Utils.Female);
                else
                    tempList.Add(countID + "" + Utils.Male);
                
                result.Add(s, tempList);
                
                countID++;
            }

            //BFS
            while (N.Count > 0)
            {
                String current = N.Dequeue();

                //if the current individual's parents are not finsihed
                //we delay processing this indvidual
                if (parentsLeft[current] > 0)
                {
                    N.Enqueue(current);
                    continue;
                }

                //NodeCodes of current individual
                List<String> currentNodeCodes = result[current];

                //If current individual has children
                if (T.ContainsKey(current))
                {
                    //Get all the children of current individual
                    List<String> children = T[current];

                    int countChildren = 0;

                    //for each child
                    foreach (String s in children)
                    {
                        if (!result.ContainsKey(s))
                            result.Add(s, new List<String>());

                        //for each NodeCode of current individual, attach the extra coding character
                        //and save it in the NodeCodes of current child

                        //Get the gender of the individual
                        int gPos = s.LastIndexOfAny(Utils.Genders);

                        foreach (String t in currentNodeCodes)
                        {
                            if (s[gPos] == Utils.Female)
                                result[s].Add(t + countChildren + Utils.Female);
                            else
                                result[s].Add(t + countChildren + Utils.Male);
                        }

                        countChildren++;

                        //reduce the in-degree of the child
                        parentsLeft[s]--;

                        //if the child is not in the queue, push it in the queue
                        if (!statusOfGraph.ContainsKey(s))
                        {
                            statusOfGraph.Add(s, 'G');
                            N.Enqueue(s);
                        }
                    }
                }
            }

            //foreach (String s in result.Keys)
            //{
            //    Console.WriteLine(s);
            //    foreach (String t in result[s])
            //    {
            //        Console.WriteLine(t);
            //    }
            //    Console.WriteLine("-----");
            //}

                    
            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="NodeCode"></param>
        /// <param name="NCs"></param>
        /// <returns></returns>
        public double CalculateNC(List<String> NodeCode, Dictionary<String, List<String>> NCs, Dictionary<String, String> InvertedNCs)
        {

            //DateTime start = DateTime.Now;

            //Determine mother's NodeCode and father's NodeCode first
            List<String> MotherNodeCode = new List<String>();
            List<String> FatherNodeCode = new List<String>();


            foreach (String s in NodeCode)
            {
                //Get the gender for parents
                int gPos = -1;
                for (int i = s.Length - 2; i >= 0; i--)
                    if (s[i] == Utils.Female || s[i] == Utils.Male)
                    {
                        gPos = i;
                        break;
                    }

                if (gPos > 0)
                {
                    if (s[gPos] == Utils.Female)
                        MotherNodeCode.Add(s.Substring(0, gPos + 1));
                    else
                        FatherNodeCode.Add(s.Substring(0, gPos + 1));
                }
                else
                {
                    if (s[s.Length - 1] == Utils.Female)
                        MotherNodeCode.Add(s);
                    else
                        FatherNodeCode.Add(s);
                }
            }

            //Check if they have inbreeding
            //Get the highest ancestors from Mother's NodeCode
            HashSet<String> mSet = new HashSet<string>();
            foreach (String s in MotherNodeCode)
            {
                String temp = s.Substring(0, s.IndexOfAny(Utils.Genders) + 1);
                mSet.Add(temp);
            }

            //Get the highest ancestors from Father's NodeCode
            HashSet<String> fSet = new HashSet<string>();
            foreach (String s in FatherNodeCode)
            {
                String temp = s.Substring(0, s.IndexOfAny(Utils.Genders) + 1);
                fSet.Add(temp);
            }

            //To see if they have any common ancestor
            mSet.IntersectWith(fSet);

            if (mSet.Count == 0)
                return 0.0;

            Dictionary<String, List<Pair>> MPSForMother = new Dictionary<string, List<Pair>>();
            Dictionary<String, List<Pair>> MPSForFather = new Dictionary<string, List<Pair>>();

            //Get the prefixes and all the paths starting with those prefixes
            foreach (String s in MotherNodeCode)
            {
                String temp = s.Substring(0, s.IndexOfAny(Utils.Genders) + 1);
                if (!mSet.Contains(temp)) continue;
                List<String> allPrefixes = Utils.AllPrefixes(s);
                for (int i = 0; i < allPrefixes.Count; i++)
                {
                    String pre = allPrefixes[i];
                    if (!MPSForMother.ContainsKey(pre))
                        MPSForMother.Add(pre, new List<Pair>());
                    MPSForMother[pre].Add(new Pair(s, allPrefixes.Count - i - 1));
                }
            }

            foreach (String s in FatherNodeCode)
            {
                String temp = s.Substring(0, s.IndexOfAny(Utils.Genders) + 1);
                if (!mSet.Contains(temp)) continue;
                List<String> allPrefixes = Utils.AllPrefixes(s);
                for (int i = 0; i < allPrefixes.Count; i++)
                {
                    String pre = allPrefixes[i];
                    if (MPSForMother.ContainsKey(pre))
                    {
                        if (!MPSForFather.ContainsKey(pre))
                            MPSForFather.Add(pre, new List<Pair>());
                        MPSForFather[pre].Add(new Pair(s, allPrefixes.Count - i - 1));
                    }
                }
            }


            //Get the intersection of these two sets
            List<String> candidates = new List<String>(MPSForFather.Keys);

            //Eliminate those uesless ancestors
            List<String> finalResult = new List<string>(candidates);
            foreach (String s in candidates)
            {
                String temp = s.Substring(0, s.Length - 1);
                int p = temp.LastIndexOfAny(Utils.Genders);
                if (p < 0) continue;
                String pre = s.Substring(0, p + 1);

                if (MPSForMother[pre].Count == MPSForMother[s].Count &&
                    MPSForFather[pre].Count == MPSForFather[s].Count)
                    finalResult.Remove(pre);
            }


            //The key is the ID of the individual
            //The value is the unique NodeCode for that individual
            Dictionary<String, String> CAT = new Dictionary<string, string>();
 
            foreach (String ca in finalResult)
            {
                String temp = InvertedNCs[ca];
                if (!CAT.ContainsKey(temp))
                    CAT.Add(temp, ca);                   
                if (ca.Length > CAT[temp].Length)
                    CAT[temp] = ca;
            }
            //TimeSpan time = DateTime.Now.Subtract(start);
            //totalTimeForParents += time.TotalMilliseconds;

            //End of identifying the common ancestors
            


            //Start to check the overlapping pairs of paths
            double result = 0.0;
            //For each common ancestors
            foreach (String ca in CAT.Keys)
            {
                String uniqueNC = CAT[ca];
                //Get all the paths from this common ancestor to mother and father
                List<Pair> PathToMother = MPSForMother[uniqueNC];
                List<Pair> PathToFather = MPSForFather[uniqueNC];

                bool[,] overlapping = new bool[PathToMother.Count, PathToFather.Count];

                //DateTime start1 = DateTime.Now;

                foreach (String cb in CAT.Keys)
                {
                    //if cb is not the parent of ca
                    if (ca != cb  && CAT[cb].Length > uniqueNC.Length)
                    {
                        //Get all the NodeCode
                        List<String> DescNodeCode = NCs[cb];

                        List<int> Ms = new List<int>();
                        List<int> Fs = new List<int>();

                        foreach (String code in DescNodeCode)
                        {
                            if (code.Length > uniqueNC.Length && code.StartsWith(uniqueNC))
                            {
                                for (int i = 0; i < PathToMother.Count; i++)
                                    if (PathToMother[i].nodeCode.StartsWith(code))
                                        Ms.Add(i);

                                for (int i = 0; i < PathToFather.Count; i++)
                                    if (PathToFather[i].nodeCode.StartsWith(code))
                                        Fs.Add(i);
                            }
                            //else if (uniqueNC.StartsWith(code))
                            //{
                            //    break;
                            //}
                        }

                        for (int i = 0; i < Ms.Count; i++)
                            for (int j = 0; j < Fs.Count; j++)
                                overlapping[Ms[i], Fs[j]] = true;
                    }
                }

                //int[] motherPathLength = new int[PathToMother.Count];
                //for (int i = 0; i < PathToMother.Count; i++)
                //    motherPathLength[i] = Utils.PathLengthNC(PathToMother[i], uniqueNC);
                //int[] fatherPathLength = new int[PathToFather.Count];
                //for (int i = 0; i < PathToFather.Count; i++)
                //    fatherPathLength[i] = Utils.PathLengthNC(PathToFather[i], uniqueNC);


                for (int i = 0; i < PathToMother.Count; i++)
                {
                    for (int j = 0; j < PathToFather.Count; j++)
                    {

                        if (!overlapping[i, j])
                        {
                            if (!ICTable.ContainsKey(ca))
                                ICTable.Add(ca, CalculateNC(NCs[ca], NCs, InvertedNCs));

                            result += Math.Pow(0.5, PathToMother[i].pathLength
                                                  + PathToFather[j].pathLength + 1)* (1.0 + ICTable[ca]);
                            //Console.WriteLine(Utils.PathLengthNC(PathToMother[i], uniqueNC) + " " + Utils.PathLengthNC(PathToFather[j], uniqueNC));

                        }
                    }
                }

                //TimeSpan time1 = DateTime.Now.Subtract(start1);
                //totalTimeForOverlapping += time1.TotalMilliseconds;
            }

            return result;            
        }

        /// <summary>
        /// Calculate the inbreeding coefficient using NodeCode method
        /// </summary>
        /// <param name="TGDL"></param>
        /// <param name="NontreeEdge"></param>
        /// <returns></returns>
        public double CalculateNC(String TGDL, String NontreeEdge)
        {
            //DateTime start = DateTime.Now;

            //If the inbreeding coefficient is existed, just return it
            if (ICTable.ContainsKey(TGDL))
                return ICTable[TGDL];

            //Fetch TGDL code from EGDL code
            String[] temp = NontreeEdge.Split(Utils.Delimiters);

            String[,] NontreeEdges = new String[(temp.Length - 1) / 2, 2];

            //Assign the non-tree edges
            for (int i = 1; i < temp.Length; i += 2)
            {
                NontreeEdges[(i - 1) / 2, 0] = temp[i];
                NontreeEdges[(i - 1) / 2, 1] = temp[i + 1];
            }

            //First identify mother and father, and the TGDL of them will be stored in the global variable
            //MotherTGDL and FatherTGDL
            String[] tempParents = IdentifyMF(TGDL, NontreeEdges);

            String FatherTGDL = "";
            String MotherTGDL = "";
            if (tempParents != null)
            {
                FatherTGDL = tempParents[0];

                MotherTGDL = tempParents[1];
            }

            if (FatherTGDL == "" || MotherTGDL == "")
                return 0.0;


            //the siblings have exactly the same inbreeding coefficient
            if (storedResults.ContainsKey(FatherTGDL) && storedResults[FatherTGDL].ContainsKey(MotherTGDL))
                return storedResults[FatherTGDL][MotherTGDL];

           
            Dictionary<String, List<String>> NCs = CPEtoSubGraph(TGDL, NontreeEdge);

            //Determine the unique common ancestors
            Dictionary<String, String> InvertedNCs = new Dictionary<string, string>();
            foreach (String key in NCs.Keys)
                foreach (String nc in NCs[key])
                    InvertedNCs.Add(nc, key);            

            //TimeSpan time = DateTime.Now.Subtract(start);
            //totalTimeForPath += time.TotalMilliseconds;

            //double tempLength = 0;
            //foreach (String s in NCs.Keys)
            //    foreach (String t in NCs[s])
            //        tempLength += t.Length;

            //if (tempLength > totalLengthNodeCode)
            //    totalLengthNodeCode = tempLength;

            List<String> NodeCode = NCs[TGDL];

            double returnResult = CalculateNC(NodeCode, NCs, InvertedNCs);

            if (!storedResults.ContainsKey(FatherTGDL))
                storedResults.Add(FatherTGDL, new Dictionary<String, double>());

            // store results in memory for future use
            if (!storedResults[FatherTGDL].ContainsKey(MotherTGDL))
                storedResults[FatherTGDL].Add(MotherTGDL, returnResult);

            return returnResult;
        }
    }
}
