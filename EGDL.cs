using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EGDL
{
    class EGDL
    {
        Graph localGraph;
        /// <summary>
        /// The constructor of EGDL class uses a database instance as its input parameter.
        /// The reason is that we have to access to the databse frequently, and it's better to open the 
        /// database once, do all the tasks and then close it.
        /// </summary>
        /// <param name="conn">The connection string of the database</param>
        public EGDL(Database conn)
        {
            //using the database conn to initilize the local graph object
            localGraph = new Graph(conn);
        }

        /// <summary>
        /// For a specific PedigreeID, encode the whole pedigree graph
        /// </summary>
        /// <param name="PID">Pedigree ID</param>
        public void EGDLEncoding(int PID)
        {
            int count = 0;

            //The key is the IndividualID of a pedigree graph
            //The value is the color of that individual, which could be White, Gray or Black
            //Since the storage of this table is linear to the number of individuals and it's only used during the encoding procedure
            //We could store it in the main memory
            Dictionary<int, char> statusOfGraph = new Dictionary<int, char>();
           
            //Add the virtual root to all the individuals without parents
            List<Individual> listOfIndividuals = localGraph.IndividualsWOParents(PID);  //start encoding from these individuals

            //If there is no such individuals that doesn't have any parents
            //we have to skip to the other pedigree
            //but this condition may not happen in the usual case
            //because the pedigree graph doesn't contain a directed circle
            if (listOfIndividuals == null)
                return;

            //The corresponding TGDL code
            List<String> TGDLCode = TGDLEncoding(listOfIndividuals.Count);

            //for all the individuals without parents
            for (int j = 0; j < listOfIndividuals.Count; j++)
            {
                //Probably we could save this code into memory in order to reduce the number of accessing database
                //The TGDL code doesn't have that delimiter
                String Tcode = (listOfIndividuals[j].Gender == 1 ? Utils.Male : Utils.Female) + TGDLCode[j];

                //Set the TGDL code
                localGraph.SetTGDL(listOfIndividuals[j].PedigreeID, listOfIndividuals[j].IndividualID, Tcode);

                //Set the Nontree Edges, although it's a empty string
                localGraph.SetNontreeEdges(listOfIndividuals[j].PedigreeID, listOfIndividuals[j].IndividualID, "");
            }


            //The above code is actually the first step of the BFS
            //Now we are going to continue doing the BFS
            Queue<Individual> q = new Queue<Individual>();

            //Enqueue all of these individuals
            foreach (Individual indi in listOfIndividuals)
            {
                q.Enqueue(indi);

                //Change the color of these individuals to Gray
                statusOfGraph.Add(indi.IndividualID, 'G');
            }

            //Start BFS
            while (q.Count > 0)
            {
                Individual indi = q.Dequeue();

                //Check if all the parents of indi have been visited
                //First check mother
                if (indi.MotherID != -1 && (!statusOfGraph.ContainsKey(indi.MotherID) || statusOfGraph[indi.MotherID] != 'B'))
                {
                    q.Enqueue(indi);
                    continue;
                }
                //Then check father
                if (indi.FatherID != -1 && (!statusOfGraph.ContainsKey(indi.FatherID) || statusOfGraph[indi.FatherID] != 'B'))
                {
                    q.Enqueue(indi);
                    continue;
                }

                //Since the TGDL code of the parent will be the prefix of its children's TGDL code
                //We have to pull this out from the database first
                String TGDLCodeofParent = localGraph.GetTGDL(indi.PedigreeID, indi.IndividualID);

                //Insert the node into the topology table
                localGraph.InsertTopology(indi.PedigreeID, count++, TGDLCodeofParent);

                //Get the children of current individual
                List<Individual> tempList = localGraph.GetChildren(indi.PedigreeID, indi.IndividualID);

                //Set the out digree
                localGraph.SetOutDegree(indi.PedigreeID, TGDLCodeofParent, tempList.Count);

                //If this individual is a leaf node, we can just skip to the next round
                //Because there is no way to expand
                if (tempList == null || tempList.Count == 0)
                    continue;

                //First get the corresponding TGDL code set
                //The size of this list could be larger than the actual needed TGDL code
                //Because some of them may have already been visited
                List<String> tempTGDLCode = TGDLEncoding(tempList.Count);

                //Get the non-tree edges of the parent node
                //Since every non-tree path to the parent will also reach its children
                //the children will inherit this non-tree edges part
                String parentNontreeEdges = localGraph.GetNontreeEdges(indi.PedigreeID, indi.IndividualID);
                if (parentNontreeEdges == null)
                    parentNontreeEdges = "";

                //An index for the used index                    
                int usedTGDL = 0;

                //for each child, do the EGDL encoding
                for (int j = 0; j < tempList.Count; j++)
                {
                    //If the current individual has not been visited
                    //then it needs TGDL encoding
                    if (!statusOfGraph.ContainsKey(tempList[j].IndividualID))  //which means that the color of the individual is White
                    //Only at this time, we need to do TGDL encoding
                    //And the edge is a tree edge
                    {
                        String TGDLCurrentPiece = (tempList[j].Gender == 1 ? Utils.Male : Utils.Female) + tempTGDLCode[usedTGDL++];

                        //The TGDL code of the current individual
                        localGraph.SetTGDL(tempList[j].PedigreeID, tempList[j].IndividualID, TGDLCodeofParent + TGDLCurrentPiece);

                        //the parentNontreeEdges is actually the non-tree edges of the indi
                        //actually we process the children of indi now, therefore we call it parentNontreeEdges                        
                        String tempS = Utils.ProcessGender(parentNontreeEdges, null, indi.Gender);

                        //First assign the parent's non-tree edges to the child
                        localGraph.SetNontreeEdges(tempList[j].PedigreeID, tempList[j].IndividualID, tempS);

                        //Don't forget to change the color of that individual
                        statusOfGraph.Add(tempList[j].IndividualID, 'G');

                        //This individual must be enqueued, because we have never visited this one before
                        q.Enqueue(tempList[j]);
                    }
                    else  //The color of the current individual is either Gray or Black
                    //The current edge must be a non-tree edge if the color of the current individual is Gray
                    {
                        //Fetch the TGDL from database
                        //Since this individual has been visited before, it must have a TGDL code
                        String TGDLCurrentPiece = localGraph.GetTGDL(tempList[j].PedigreeID, tempList[j].IndividualID);

                        //The new non-tree edge part
                        //we use a special symbol to represent the newly added non-tree edge
                        String newNontreeEdge = Utils.DelimiterSpecial + TGDLCodeofParent + Utils.DelimiterSpecial + TGDLCurrentPiece;
                        if (statusOfGraph[indi.IndividualID] == 'B')
                            newNontreeEdge = "";

                        //Get the non-tree edges of the current individual
                        String NontreeEdgeCurrentPiece = localGraph.GetNontreeEdges(tempList[j].PedigreeID, tempList[j].IndividualID);

                        String NewNontreeEdgePart = Utils.ProcessGender(parentNontreeEdges, NontreeEdgeCurrentPiece, indi.Gender) + newNontreeEdge;  //Get the union of the three strings
                        
                        if (localGraph.GetNontreeEdges(tempList[j].PedigreeID, tempList[j].IndividualID) != null)                            
                            localGraph.UpdateNontreeEdges(tempList[j].PedigreeID, tempList[j].IndividualID, NewNontreeEdgePart);
                        else
                            localGraph.SetNontreeEdges(tempList[j].PedigreeID, tempList[j].IndividualID, NewNontreeEdgePart);
                    }

                }

                //Mark the individual as already expanded
                statusOfGraph[indi.IndividualID] = 'B';
            }
        }

        /// <summary>
        /// A overload method to encode all the pedigrees in the database
        /// </summary>
        public void EGDLEncoding()
        {
            //If there are multiple pedigree graphs, we have to encode them one by one
            //First determine the number of different pedigree graphs
            List<int> PIDs = localGraph.NumberofPedigree();
            
            //For each pedigree graph, do the encoding
            for (int i = 0; i < PIDs.Count; i++)
            {
                EGDLEncoding(PIDs[i]);
            }
        }

        /// <summary>
        /// This method will produce a list of string which represents the TGDL encoding
        /// for the length of "count"
        /// </summary>
        /// <param name="count">How many TGDL do you need?</param>
        /// <returns></returns>
        public List<String> TGDLEncoding(int count)
        {
            if (count == 0)
                return null;
            else
            {
                List<String> r = new List<String>();
                
                for (int i = 0; i < count; i++)
                {
                    r.Add(Utils.ToBase64(i));
                }
                return r;
            }
        }
    }
}
