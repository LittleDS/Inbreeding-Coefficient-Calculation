using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EGDL
{
    class Utils
    {
        public static char Delimiter = '*';
        public static char DelimiterMale = '#';
        public static char DelimiterFemale = '$';
        public static char DelimiterBoth = '!';
        public static char DelimiterSpecial = '%';
        public static char[] Delimiters = {'*','#','$','!','%'};

        public static char Male = '.';
        public static char Female = ',';
        public static char[] Genders = { '.', ',' };

        public static char[] candidate = {'0','1','2','3','4','5','6','7','8','9',
                                    'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
                                    'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','^','~'};

        /// <summary>
        /// Convert a decimal number to Base64
        /// </summary>
        /// <param name="number">The number</param>
        /// <returns></returns>
        public static String ToBase64(int number)
        {
            if (number == 0)
                return "0";

            String r = "";
            while (number != 0)
            {
                r += candidate[number % 64];
                number /= 64;
            }

            String s = "";
            for (int i = r.Length - 1; i >= 0; i--)
                s += r[i];

            return s;                
        }

        /// <summary>
        /// This method is used to calculate the union of three different strings
        /// </summary>
        /// <param name="A">A string representing multiple edges</param>
        /// <param name="B">Another string representing multiple edges</param>
        /// <param name="C">A third string representing only one edge</param>
        /// <returns></returns>
        public static String StringUnion(String A, String B, String C)
        {
            if (A == null)
                A = "";
            if (B == null)
                B = "";
            if (C == null)
                C = "";
            
            //We can simply attach C to B
            B += C;

            String[] Aa = A.Split(Delimiter);
            String[] Ba = B.Split(Delimiter);
            
            int lena = Aa.Length;
            int lenb = Ba.Length;
            
            String r = A;

            for (int i = 1; i <= lenb / 2; i++)
            {
                bool flag = false;
                for (int j = 1; j <= lena / 2; j++)
                {
                    if (Ba[2 * i - 1] == Aa[2 * j - 1] && Ba[2 * i] == Aa[2 * j])
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                    r += Delimiter + Ba[2 * i - 1] + Delimiter + Ba[2 * i];
            }

            return r;
        }


        /// <summary>
        /// Return if String A is a prefix of String B
        /// </summary>
        /// <param name="A">A should be the shorter one</param>
        /// <param name="B">B is the longer one</param>
        /// <returns>True if A is a prefix of B</returns>
        public static bool IsPrefix(String A, String B)
        {
            //If String A is longer than String B, it cannot be a prefix of B
            if (A.Length > B.Length)
                return false;

            bool flag = true;
            String[] tempA = A.Split(Utils.Genders);
            String[] tempB = B.Split(Utils.Genders);
            
            for (int i = 1; i < tempA.Length; i++)
            {
                //If one of the chars in A is not equal to the corresponding char in B
                if (tempA[i] != tempB[i])
                {
                    flag = false;
                    break;
                }
            }

            return flag;
        }


        /// <summary>
        /// Extend a path from to strings.
        /// Before calling this method, you have to make sure that A is a prefix of B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns>A list of strings representing the path</returns>
        public static List<String> ExtendPath(String A, String B)
        {
            List<String> r = new List<String>();
            int j = A.Length;
            while (j <= B.Length)
            {
                //Get the position of next symbol of Male of Female
                while (j < B.Length && B[j] != Utils.Female && B[j] != Utils.Male)
                    j++;

                //Add the piece as a point in the path
                String tempPiece = B.Substring(0, j);
                r.Add(tempPiece);

                //Skip the symbol of Male and Femal
                j++;
            }

            return r;
        }


        /// <summary>
        /// This method will connect the single former path to a set of paths.
        /// </summary>
        /// <param name="formerPath">The former single path</param>
        /// <param name="latterPaths">A set of paths which will be attached to the former path</param>
        /// <returns>A List of connected paths</returns>
        public static List<List<String>> ConnectPaths(List<String> formerPath, List<List<String>> latterPaths)
        {
            //The return result
            List<List<String>> r = new List<List<String>>();;
            
            //There is a condition that the latterpaths is empty
            //In such case, the only thing you need to do is to return the formerPath as the result
            if (latterPaths.Count > 0)
            {
                for (int i = 0; i < latterPaths.Count; i++)
                {
                    //A temp path =
                    List<String> temp = new List<String>();

                    //First add the former path
                    temp.AddRange(formerPath);

                    for (int j = 1; j < latterPaths[i].Count; j++)
                        temp.Add(latterPaths[i][j]);

                    //Add the connected path to the result set
                    r.Add(temp);
                }
            }
            else
                r.Add(formerPath);

            return r;
        }


        /// <summary>
        /// Split the non-tree edges into triples
        /// </summary>
        /// <param name="parentString">the input string</param>
        /// <returns></returns>
        public static List<Triple> SplitString(String parentString)
        {
            //We have to attach another delimiter to the end of the string
            //to terminate the processing of the current string
            parentString += Utils.DelimiterBoth;

            List<Triple> r = new List<Triple>();
            int len = parentString.Length;
            int count = 0;
            int startPos = 0;
            int midPos = 0;
            for (int i = 0; i < len; i++)
            {
                if (parentString[i] == Utils.Delimiter ||
                    parentString[i] == Utils.DelimiterFemale ||
                    parentString[i] == Utils.DelimiterMale ||
                    parentString[i] == Utils.DelimiterBoth ||
                    parentString[i] == Utils.DelimiterSpecial)
                {
                    count++;
                    if (count == 1)
                    {
                        startPos = i;
                    } 
                    else if (count == 2) 
                    {
                        midPos = i;
                    }
                    else if (count == 3)
                    {
                        count = 1;
                        String s = parentString.Substring(startPos + 1, midPos - startPos - 1);
                        String d = parentString.Substring(midPos + 1, i - midPos - 1);
                        r.Add(new Triple(s, d, parentString[midPos]));
                        startPos = i;
                    }
                }
            }

            return r;
        }

        /// <summary>
        /// Process the gender of the non-tree edges
        /// </summary>
        /// <param name="parentString"></param>
        /// <param name="indiString"></param>
        /// <param name="Gender"></param>
        /// <returns></returns>
        public static String ProcessGender(String parentString, String indiString, int Gender)
        {
            String R = "";
            //The delimiter to represent different gender
            char GenderSymbol = (Gender == 1 ? Utils.DelimiterMale : Utils.DelimiterFemale);

            //If this is the first time that one individual get non-tree edges
            //from its parent
            if (indiString == null)
            {
                List<Triple> temp = SplitString(parentString);
                foreach (Triple t in temp)
                {
                    R += GenderSymbol + t.source + GenderSymbol + t.destination;
                }
                return R;
            }
            else  //this is not the first time
                  //therefore we have to merge the non-tree edges from its parents properly
            {
                HashSet<Triple> result = new HashSet<Triple>();
                List<Triple> parent = SplitString(parentString);
                List<Triple> indi = SplitString(indiString);

                //First add the non-tree edges of the current individual to the result set
                foreach (Triple t in indi)
                {                  
                    result.Add(t);
                }

                //Second, try to merge the non-tree edges from the parent
                foreach (Triple t in parent)
                {
                    //if the result set already contains the current non-tree edge
                    //we have to update the gender to indicate that this non-tree edge
                    //is from both parents
                    if (result.Contains(t))
                    {
                        result.Remove(t);
                        t.Gender = Utils.DelimiterBoth;
                        result.Add(t);
                    }
                    else  //otherwise we simply add it to the result set
                    {
                        Triple tempT = new Triple(t.source, t.destination, GenderSymbol);
                        result.Add(tempT);
                    }
                }

                //output the result set as a string
                foreach (Triple t in result)
                {
                    R += t.Gender + t.source + t.Gender + t.destination;
                }

                return R;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="TGDL"></param>
        /// <param name="NontreeEdges">The NontreeEdge of the child</param>
        /// <param name="Gender"></param>
        /// <returns></returns>
        public static String GetNontreeEdgeFromTGDL(String TGDL, String NontreeEdges, int Gender)
        {
            String R = "";
            List<Triple> ts = Utils.SplitString(NontreeEdges);
            foreach (Triple t in ts)
            {
                //if the edges is not conflict with the gender
                if ((t.Gender == Utils.DelimiterMale && Gender == 2) ||
                   (t.Gender == Utils.DelimiterFemale && Gender == 1) ||
                   (t.Gender == Utils.DelimiterSpecial))
                {
                    continue;
                }
                else
                {
                    R += Utils.Delimiter + t.source + Utils.Delimiter + t.destination;
                }
            }

            return R;
        }


        /// <summary>
        /// Given a Nodecode return all the prefixes
        /// </summary>
        /// <param name="NodeCode"></param>
        /// <returns></returns>
        public static List<String> AllPrefixes(String NodeCode)
        {
            List<String> result = new List<String>();
            for (int i = 0; i < NodeCode.Length; i++)
            {
                if (NodeCode[i] == Utils.Female || NodeCode[i] == Utils.Male)
                {
                    result.Add(NodeCode.Substring(0, i + 1));
                }
            }
            return result;
        }

        /// <summary>
        /// Given the NodeCodes, return all the paths starting with uniqueNC
        /// </summary>
        /// <param name="uniqueNC"></param>
        /// <param name="NC"></param>
        /// <returns></returns>
        public static List<String> GetAllPaths(String uniqueNC, List<String> NC)
        {
            List<String> result = new List<string>();
            foreach (String s in NC)
            {
                if (s.StartsWith(uniqueNC))
                    result.Add(s);
            }
            return result;
        }


        /// <summary>
        /// Get the length of a path
        /// </summary>
        /// <param name="ancestor"></param>
        /// <param name="descendant"></param>
        /// <returns></returns>
        public static int PathLengthNC(String descendant, String ancestor)
        {
            String temp = descendant.Substring(ancestor.Length, descendant.Length - ancestor.Length);
            
            int result = 0;

            for (int i = 0; i < temp.Length; i++)
                if (temp[i] == Utils.Female || temp[i] == Utils.Male)
                    result++;

            return result;
        }
        
        /// <summary>
        /// Return the longest common prefix of two strings
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static String LongestCommonPrefix(String a, String b)
        {
            int l = a.Length;
            int n = b.Length;
            int i = 0;
            string result = "";
            while (i < l && i < n)
            {
                if (a[i] == b[i])
                {
                    if (a[i] == Utils.Male || a[i] == Utils.Female)
                        result = a.Substring(0, i + 1);
                    i++;
                }
                else
                    break;
            }

            return result;
        }
    }
}
