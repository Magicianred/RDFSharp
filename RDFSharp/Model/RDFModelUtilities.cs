﻿/*
   Copyright 2012-2020 Marco De Salvo

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace RDFSharp.Model
{

    /// <summary>
    /// RDFModelUtilities is a collector of reusable utility methods for RDF model management
    /// </summary>
    public static class RDFModelUtilities
    {

        #region Hashing
        /// <summary>
        /// Creates a unique long representation of the given string
        /// </summary>
        public static long CreateHash(string input)
        {
            if (input == null)
                throw new RDFModelException("Cannot create hash because given \"input\" string parameter is null.");

            using (MD5CryptoServiceProvider md5Encryptor = new MD5CryptoServiceProvider())
            {
                byte[] hashBytes = md5Encryptor.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToInt64(hashBytes, 0);
            }
        }
        #endregion

        #region Strings
        /// <summary>
        /// Regex to catch 8-byte unicodes
        /// </summary>
        internal static readonly Regex regexU8 = new Regex(@"\\U([0-9A-Fa-f]{8})", RegexOptions.Compiled);
        /// <summary>
        /// Regex to catch 4-byte unicodes
        /// </summary>
        internal static readonly Regex regexU4 = new Regex(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);

        /// <summary>
        /// Gets the Uri corresponding to the given string
        /// </summary>
        internal static Uri GetUriFromString(string uriString)
        {
            Uri tempUri = null;
            if (uriString != null)
            {

                // blank detection
                if (uriString.StartsWith("_:"))
                    uriString = string.Concat("bnode:", uriString.Substring(2));

                Uri.TryCreate(uriString, UriKind.Absolute, out tempUri);

            }
            return tempUri;
        }

        /// <summary>
        /// Searches the given Uri in the namespace register for getting its dereferencable representation;<br/>
        /// if not found, just returns the given Uri
        /// </summary>
        internal static Uri RemapUriForDereference(Uri uri)
        {
            string uriString = uri?.ToString() ?? string.Empty;

            RDFNamespace rdfNamespace = RDFNamespaceRegister.GetByUri(uriString);
            if (rdfNamespace != null)
                return rdfNamespace.DereferenceUri;

            return uri;
        }

        /// <summary>
        /// Turns back ASCII-encoded Unicodes into Unicodes.
        /// </summary>
        public static string ASCII_To_Unicode(string asciiString)
        {
            if (asciiString != null)
            {
                asciiString = regexU8.Replace(asciiString, match => ((char)long.Parse(match.Groups[1].Value, NumberStyles.HexNumber)).ToString(CultureInfo.InvariantCulture));
                asciiString = regexU4.Replace(asciiString, match => ((char)int.Parse(match.Groups[1].Value, NumberStyles.HexNumber)).ToString(CultureInfo.InvariantCulture));
            }
            return asciiString;
        }

        /// <summary>
        /// Turns Unicodes into ASCII-encoded Unicodes.
        /// </summary>
        public static string Unicode_To_ASCII(string unicodeString)
        {
            if (unicodeString != null)
            {
                StringBuilder b = new StringBuilder();
                foreach (char c in unicodeString)
                {
                    if (c <= 127)
                    {
                        b.Append(c);
                    }
                    else
                    {
                        if (c <= 65535)
                        {
                            b.Append(string.Concat("\\u", ((int)c).ToString("X4")));
                        }
                        else
                        {
                            b.Append(string.Concat("\\U", ((int)c).ToString("X8")));
                        }
                    }
                }
                unicodeString = b.ToString();
            }
            return unicodeString;
        }

        /// <summary>
        /// Replaces character controls for XML compatibility
        /// </summary>
        internal static string EscapeControlCharsForXML(string data)
        {
            if (data != null)
            {
                StringBuilder b = new StringBuilder();
                foreach (char c in data)
                {
                    if (char.IsControl(c) && c != '\u0009' && c != '\u000A' && c != '\u000D')
                    {
                        b.Append(string.Concat("\\u", ((int)c).ToString("X4")));
                    }
                    else
                    {
                        b.Append(c);
                    }
                }
                data = b.ToString();
            }
            return data;
        }

        /// <summary>
        /// Trims the end of the given source string searching for the given value
        /// </summary>
        internal static string TrimEnd(this string source, string value)
        {
            if (!source.EndsWith(value))
                return source;

            return source.Remove(source.LastIndexOf(value));
        }
        #endregion

        #region Graph
        /// <summary>
        /// Selects the triples corresponding to the given pattern from the given graph
        /// </summary>
        internal static List<RDFTriple> SelectTriples(RDFGraph graph, RDFResource subj, RDFResource pred, RDFResource obj, RDFLiteral lit)
        {
            var matchSubj = new List<RDFTriple>();
            var matchPred = new List<RDFTriple>();
            var matchObj = new List<RDFTriple>();
            var matchLit = new List<RDFTriple>();
            var matchResult = new List<RDFTriple>();
            if (graph != null)
            {

                //Filter by Subject
                if (subj != null)
                {
                    foreach (var t in graph.GraphIndex.SelectIndexBySubject(subj))
                    {
                        matchSubj.Add(graph.Triples[t]);
                    }
                }

                //Filter by Predicate
                if (pred != null)
                {
                    foreach (var t in graph.GraphIndex.SelectIndexByPredicate(pred))
                    {
                        matchPred.Add(graph.Triples[t]);
                    }
                }

                //Filter by Object
                if (obj != null)
                {
                    foreach (var t in graph.GraphIndex.SelectIndexByObject(obj))
                    {
                        matchObj.Add(graph.Triples[t]);
                    }
                }

                //Filter by Literal
                if (lit != null)
                {
                    foreach (var t in graph.GraphIndex.SelectIndexByLiteral(lit))
                    {
                        matchLit.Add(graph.Triples[t]);
                    }
                }

                //Intersect the filters
                if (subj != null)
                {
                    if (pred != null)
                    {
                        if (obj != null)
                        {
                            //S->P->O
                            matchResult = matchSubj.Intersect(matchPred)
                                                   .Intersect(matchObj)
                                                   .ToList();
                        }
                        else
                        {
                            if (lit != null)
                            {
                                //S->P->L
                                matchResult = matchSubj.Intersect(matchPred)
                                                       .Intersect(matchLit)
                                                       .ToList();
                            }
                            else
                            {
                                //S->P->
                                matchResult = matchSubj.Intersect(matchPred)
                                                       .ToList();
                            }
                        }
                    }
                    else
                    {
                        if (obj != null)
                        {
                            //S->->O
                            matchResult = matchSubj.Intersect(matchObj)
                                                   .ToList();
                        }
                        else
                        {
                            if (lit != null)
                            {
                                //S->->L
                                matchResult = matchSubj.Intersect(matchLit)
                                                       .ToList();
                            }
                            else
                            {
                                //S->->
                                matchResult = matchSubj;
                            }
                        }
                    }
                }
                else
                {
                    if (pred != null)
                    {
                        if (obj != null)
                        {
                            //->P->O
                            matchResult = matchPred.Intersect(matchObj)
                                                   .ToList();
                        }
                        else
                        {
                            if (lit != null)
                            {
                                //->P->L
                                matchResult = matchPred.Intersect(matchLit)
                                                       .ToList();
                            }
                            else
                            {
                                //->P->
                                matchResult = matchPred;
                            }
                        }
                    }
                    else
                    {
                        if (obj != null)
                        {
                            //->->O
                            matchResult = matchObj;
                        }
                        else
                        {
                            if (lit != null)
                            {
                                //->->L
                                matchResult = matchLit;
                            }
                            else
                            {
                                //->->
                                matchResult = graph.Triples.Values.ToList();
                            }
                        }
                    }
                }

            }
            return matchResult;
        }
        #endregion

        #region Collections
        /// <summary>
        /// Rebuilds the collection represented by the given resource within the given graph
        /// </summary>
        internal static RDFCollection DeserializeCollectionFromGraph(RDFGraph graph,
                                                                     RDFResource collRepresentative,
                                                                     RDFModelEnums.RDFTripleFlavors expectedFlavor)
        {
            RDFCollection collection = new RDFCollection(expectedFlavor == RDFModelEnums.RDFTripleFlavors.SPO ? RDFModelEnums.RDFItemTypes.Resource :
                                                                                                                RDFModelEnums.RDFItemTypes.Literal);
            RDFGraph rdfFirst = graph.SelectTriplesByPredicate(RDFVocabulary.RDF.FIRST);
            RDFGraph rdfRest = graph.SelectTriplesByPredicate(RDFVocabulary.RDF.REST);

            #region Deserialization
            bool nilFound = false;
            RDFResource itemRest = collRepresentative;
            HashSet<long> itemRestVisitCache = new HashSet<long>() { itemRest.PatternMemberID };
            while (!nilFound)
            {

                #region rdf:first
                RDFTriple first = rdfFirst.SelectTriplesBySubject(itemRest).FirstOrDefault();
                if (first != null && first.TripleFlavor == expectedFlavor)
                {
                    if (expectedFlavor == RDFModelEnums.RDFTripleFlavors.SPO)
                        collection.AddItem((RDFResource)first.Object);
                    else
                        collection.AddItem((RDFLiteral)first.Object);
                }
                else
                {
                    nilFound = true;
                }
                #endregion

                #region rdf:rest
                RDFTriple rest = rdfRest.SelectTriplesBySubject(itemRest).FirstOrDefault();
                if (rest != null)
                {
                    if (rest.Object.Equals(RDFVocabulary.RDF.NIL))
                        nilFound = true;
                    else
                    {
                        itemRest = (RDFResource)rest.Object;
                        //Avoid bad-formed cyclic lists to generate infinite loops
                        if (!itemRestVisitCache.Contains(itemRest.PatternMemberID))
                            itemRestVisitCache.Add(itemRest.PatternMemberID);
                        else
                            nilFound = true;
                    }
                }
                else
                {
                    nilFound = true;
                }
                #endregion

            }
            #endregion

            return collection;
        }

        /// <summary>
        /// Detects the flavor (SPO/SPL) of the collection represented by the given resource within the given graph
        /// </summary>
        internal static RDFModelEnums.RDFTripleFlavors DetectCollectionFlavorFromGraph(RDFGraph graph,
                                                                                       RDFResource collRepresentative)
        {
            return graph.SelectTriplesBySubject(collRepresentative)
                        .SelectTriplesByPredicate(RDFVocabulary.RDF.FIRST)
                        .FirstOrDefault()
                       ?.TripleFlavor ?? RDFModelEnums.RDFTripleFlavors.SPO;
        }
        #endregion

        #region Namespaces
        /// <summary>
        /// Gets the list of namespaces used within the triples of the given graph
        /// </summary>
        internal static List<RDFNamespace> GetGraphNamespaces(RDFGraph graph)
        {
            var result = new List<RDFNamespace>();
            foreach (var t in graph)
            {
                var subj = t.Subject.ToString();
                var pred = t.Predicate.ToString();
                var obj = t.Object is RDFResource ? t.Object.ToString() :
                                (t.Object is RDFTypedLiteral ? GetDatatypeFromEnum(((RDFTypedLiteral)t.Object).Datatype) : string.Empty);

                //Resolve subject Uri
                var subjNS = RDFNamespaceRegister.Instance.Register.Where(x => subj.StartsWith(x.ToString()));

                //Resolve predicate Uri
                var predNS = RDFNamespaceRegister.Instance.Register.Where(x => pred.StartsWith(x.ToString()));

                //Resolve object Uri
                var objNS = RDFNamespaceRegister.Instance.Register.Where(x => obj.StartsWith(x.ToString()));

                result.AddRange(subjNS);
                result.AddRange(predNS);
                result.AddRange(objNS);
            }
            return result.Distinct().ToList();
        }
        #endregion

        #region Datatypes
        /// <summary>
        /// Parses the given string in order to give the corresponding RDF/RDFS/XSD datatype
        /// </summary>
        public static RDFModelEnums.RDFDatatypes GetDatatypeFromString(string datatypeString)
        {
            if (datatypeString != null)
            {

                //Preliminary check to verify if datatypeString is a valid Uri
                if (!Uri.TryCreate(datatypeString.Trim(), UriKind.Absolute, out Uri dtypeStringUri))
                    throw new RDFModelException("Cannot recognize datatype representation of given \"datatypeString\" parameter because it is not a valid absolute Uri.");

                //Identification of specific RDF/RDFS/XSD datatype
                datatypeString = dtypeStringUri.ToString();
                if (datatypeString.Equals(RDFVocabulary.RDF.XML_LITERAL.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.RDF_XMLLITERAL;
                else if (datatypeString.Equals(RDFVocabulary.RDF.HTML.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.RDF_HTML;
                else if (datatypeString.Equals(RDFVocabulary.RDF.JSON.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.RDF_JSON;
                else if (datatypeString.Equals(RDFVocabulary.RDFS.LITERAL.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.RDFS_LITERAL;
                else if (datatypeString.Equals(RDFVocabulary.XSD.STRING.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_STRING;
                else if (datatypeString.Equals(RDFVocabulary.XSD.ANY_URI.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_ANYURI;
                else if (datatypeString.Equals(RDFVocabulary.XSD.BASE64_BINARY.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_BASE64BINARY;
                else if (datatypeString.Equals(RDFVocabulary.XSD.BOOLEAN.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_BOOLEAN;
                else if (datatypeString.Equals(RDFVocabulary.XSD.BYTE.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_BYTE;
                else if (datatypeString.Equals(RDFVocabulary.XSD.DATE.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_DATE;
                else if (datatypeString.Equals(RDFVocabulary.XSD.DATETIME.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_DATETIME;
                else if (datatypeString.Equals(RDFVocabulary.XSD.DECIMAL.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_DECIMAL;
                else if (datatypeString.Equals(RDFVocabulary.XSD.DOUBLE.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_DOUBLE;
                else if (datatypeString.Equals(RDFVocabulary.XSD.DURATION.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_DURATION;
                else if (datatypeString.Equals(RDFVocabulary.XSD.FLOAT.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_FLOAT;
                else if (datatypeString.Equals(RDFVocabulary.XSD.G_DAY.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_GDAY;
                else if (datatypeString.Equals(RDFVocabulary.XSD.G_MONTH.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_GMONTH;
                else if (datatypeString.Equals(RDFVocabulary.XSD.G_MONTH_DAY.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_GMONTHDAY;
                else if (datatypeString.Equals(RDFVocabulary.XSD.G_YEAR.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_GYEAR;
                else if (datatypeString.Equals(RDFVocabulary.XSD.G_YEAR_MONTH.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_GYEARMONTH;
                else if (datatypeString.Equals(RDFVocabulary.XSD.HEX_BINARY.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_HEXBINARY;
                else if (datatypeString.Equals(RDFVocabulary.XSD.ID.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_ID;
                else if (datatypeString.Equals(RDFVocabulary.XSD.INT.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_INT;
                else if (datatypeString.Equals(RDFVocabulary.XSD.INTEGER.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_INTEGER;
                else if (datatypeString.Equals(RDFVocabulary.XSD.LANGUAGE.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_LANGUAGE;
                else if (datatypeString.Equals(RDFVocabulary.XSD.LONG.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_LONG;
                else if (datatypeString.Equals(RDFVocabulary.XSD.NAME.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_NAME;
                else if (datatypeString.Equals(RDFVocabulary.XSD.NCNAME.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_NCNAME;
                else if (datatypeString.Equals(RDFVocabulary.XSD.NEGATIVE_INTEGER.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_NEGATIVEINTEGER;
                else if (datatypeString.Equals(RDFVocabulary.XSD.NMTOKEN.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_NMTOKEN;
                else if (datatypeString.Equals(RDFVocabulary.XSD.NON_NEGATIVE_INTEGER.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_NONNEGATIVEINTEGER;
                else if (datatypeString.Equals(RDFVocabulary.XSD.NON_POSITIVE_INTEGER.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_NONPOSITIVEINTEGER;
                else if (datatypeString.Equals(RDFVocabulary.XSD.NORMALIZED_STRING.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_NORMALIZEDSTRING;
                else if (datatypeString.Equals(RDFVocabulary.XSD.NOTATION.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_NOTATION;
                else if (datatypeString.Equals(RDFVocabulary.XSD.POSITIVE_INTEGER.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_POSITIVEINTEGER;
                else if (datatypeString.Equals(RDFVocabulary.XSD.QNAME.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_QNAME;
                else if (datatypeString.Equals(RDFVocabulary.XSD.SHORT.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_SHORT;
                else if (datatypeString.Equals(RDFVocabulary.XSD.TIME.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_TIME;
                else if (datatypeString.Equals(RDFVocabulary.XSD.TOKEN.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_TOKEN;
                else if (datatypeString.Equals(RDFVocabulary.XSD.UNSIGNED_BYTE.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDBYTE;
                else if (datatypeString.Equals(RDFVocabulary.XSD.UNSIGNED_INT.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDINT;
                else if (datatypeString.Equals(RDFVocabulary.XSD.UNSIGNED_LONG.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDLONG;
                else if (datatypeString.Equals(RDFVocabulary.XSD.UNSIGNED_SHORT.ToString(), StringComparison.Ordinal))
                    return RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDSHORT;
                else
                    //Unknown datatypes are threated as rdfs:Literal
                    return RDFModelEnums.RDFDatatypes.RDFS_LITERAL;

            }
            throw new RDFModelException("Cannot recognize datatype representation of given \"datatypeString\" parameter because it is null.");
        }

        /// <summary>
        /// Gives the string representation of the given RDF/RDFS/XSD datatype
        /// </summary>
        public static string GetDatatypeFromEnum(RDFModelEnums.RDFDatatypes datatype)
        {
            switch (datatype)
            {
                case RDFModelEnums.RDFDatatypes.RDF_XMLLITERAL:
                    return RDFVocabulary.RDF.XML_LITERAL.ToString();
                case RDFModelEnums.RDFDatatypes.RDF_HTML:
                    return RDFVocabulary.RDF.HTML.ToString();
                case RDFModelEnums.RDFDatatypes.RDF_JSON:
                    return RDFVocabulary.RDF.JSON.ToString();
                case RDFModelEnums.RDFDatatypes.RDFS_LITERAL:
                    return RDFVocabulary.RDFS.LITERAL.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_STRING:
                    return RDFVocabulary.XSD.STRING.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_ANYURI:
                    return RDFVocabulary.XSD.ANY_URI.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_BASE64BINARY:
                    return RDFVocabulary.XSD.BASE64_BINARY.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_BOOLEAN:
                    return RDFVocabulary.XSD.BOOLEAN.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_BYTE:
                    return RDFVocabulary.XSD.BYTE.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_DATE:
                    return RDFVocabulary.XSD.DATE.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_DATETIME:
                    return RDFVocabulary.XSD.DATETIME.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_DECIMAL:
                    return RDFVocabulary.XSD.DECIMAL.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_DOUBLE:
                    return RDFVocabulary.XSD.DOUBLE.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_DURATION:
                    return RDFVocabulary.XSD.DURATION.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_FLOAT:
                    return RDFVocabulary.XSD.FLOAT.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_GDAY:
                    return RDFVocabulary.XSD.G_DAY.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_GMONTH:
                    return RDFVocabulary.XSD.G_MONTH.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_GMONTHDAY:
                    return RDFVocabulary.XSD.G_MONTH_DAY.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_GYEAR:
                    return RDFVocabulary.XSD.G_YEAR.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_GYEARMONTH:
                    return RDFVocabulary.XSD.G_YEAR_MONTH.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_HEXBINARY:
                    return RDFVocabulary.XSD.HEX_BINARY.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_INT:
                    return RDFVocabulary.XSD.INT.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_INTEGER:
                    return RDFVocabulary.XSD.INTEGER.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_LANGUAGE:
                    return RDFVocabulary.XSD.LANGUAGE.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_LONG:
                    return RDFVocabulary.XSD.LONG.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_NAME:
                    return RDFVocabulary.XSD.NAME.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_NCNAME:
                    return RDFVocabulary.XSD.NCNAME.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_ID:
                    return RDFVocabulary.XSD.ID.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_NEGATIVEINTEGER:
                    return RDFVocabulary.XSD.NEGATIVE_INTEGER.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_NMTOKEN:
                    return RDFVocabulary.XSD.NMTOKEN.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_NONNEGATIVEINTEGER:
                    return RDFVocabulary.XSD.NON_NEGATIVE_INTEGER.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_NONPOSITIVEINTEGER:
                    return RDFVocabulary.XSD.NON_POSITIVE_INTEGER.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_NORMALIZEDSTRING:
                    return RDFVocabulary.XSD.NORMALIZED_STRING.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_NOTATION:
                    return RDFVocabulary.XSD.NOTATION.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_POSITIVEINTEGER:
                    return RDFVocabulary.XSD.POSITIVE_INTEGER.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_QNAME:
                    return RDFVocabulary.XSD.QNAME.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_SHORT:
                    return RDFVocabulary.XSD.SHORT.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_TIME:
                    return RDFVocabulary.XSD.TIME.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_TOKEN:
                    return RDFVocabulary.XSD.TOKEN.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDBYTE:
                    return RDFVocabulary.XSD.UNSIGNED_BYTE.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDINT:
                    return RDFVocabulary.XSD.UNSIGNED_INT.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDLONG:
                    return RDFVocabulary.XSD.UNSIGNED_LONG.ToString();
                case RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDSHORT:
                    return RDFVocabulary.XSD.UNSIGNED_SHORT.ToString();

                //Unrecognized datatypes are threated as rdfs:Literal
                default:
                    return RDFVocabulary.RDFS.LITERAL.ToString();
            }
        }

        /// <summary>
        /// Validates the value of the given typed literal against its datatype
        /// </summary>
        internal static bool ValidateTypedLiteral(RDFTypedLiteral typedLiteral)
        {
            if (typedLiteral == null)
                throw new RDFModelException("Cannot validate RDFTypedLiteral because given \"typedLiteral\" parameter is null.");

            switch (typedLiteral.Datatype)
            {
                #region STRING CATEGORY
                case RDFModelEnums.RDFDatatypes.RDFS_LITERAL:
                case RDFModelEnums.RDFDatatypes.XSD_STRING:
                case RDFModelEnums.RDFDatatypes.RDF_HTML:
                default:
                    return true;

                case RDFModelEnums.RDFDatatypes.RDF_XMLLITERAL:
                    try
                    {
                        XDocument.Parse(typedLiteral.Value);
                        return true;
                    }
                    catch { return false; }

                case RDFModelEnums.RDFDatatypes.RDF_JSON:
                    return (typedLiteral.Value.StartsWith("{") && typedLiteral.Value.EndsWith("}"))
                                || (typedLiteral.Value.StartsWith("[") && typedLiteral.Value.EndsWith("]"));

                case RDFModelEnums.RDFDatatypes.XSD_ANYURI:
                    if (Uri.TryCreate(typedLiteral.Value, UriKind.Absolute, out Uri outUri))
                    {
                        typedLiteral.Value = Convert.ToString(outUri);
                        return true;
                    }
                    return false;

                case RDFModelEnums.RDFDatatypes.XSD_NAME:
                    try
                    {
                        XmlConvert.VerifyName(typedLiteral.Value);
                        return true;
                    }
                    catch { return false; }

                case RDFModelEnums.RDFDatatypes.XSD_QNAME:
                    string[] prefixedQName = typedLiteral.Value.Split(':');
                    if (prefixedQName.Length == 1)
                    {
                        try
                        {
                            XmlConvert.VerifyNCName(prefixedQName[0]);
                            return true;
                        }
                        catch { return false; }
                    }
                    else if (prefixedQName.Length == 2)
                    {
                        try
                        {
                            XmlConvert.VerifyNCName(prefixedQName[0]);
                            XmlConvert.VerifyNCName(prefixedQName[1]);
                            return true;
                        }
                        catch { return false; }
                    }
                    else { return false; }

                case RDFModelEnums.RDFDatatypes.XSD_NCNAME:
                case RDFModelEnums.RDFDatatypes.XSD_ID:
                    try
                    {
                        XmlConvert.VerifyNCName(typedLiteral.Value);
                        return true;
                    }
                    catch { return false; }

                case RDFModelEnums.RDFDatatypes.XSD_TOKEN:
                    try
                    {
                        XmlConvert.VerifyTOKEN(typedLiteral.Value);
                        return true;
                    }
                    catch { return false; }

                case RDFModelEnums.RDFDatatypes.XSD_NMTOKEN:
                    try
                    {
                        XmlConvert.VerifyNMTOKEN(typedLiteral.Value);
                        return true;
                    }
                    catch { return false; }

                case RDFModelEnums.RDFDatatypes.XSD_NORMALIZEDSTRING:
                    return typedLiteral.Value.IndexOfAny(new char[] { '\n', '\r', '\t' }) == -1;

                case RDFModelEnums.RDFDatatypes.XSD_LANGUAGE:
                    return Regex.IsMatch(typedLiteral.Value, "^[a-zA-Z]{1,8}(-[a-zA-Z0-9]{1,8})*$", RegexOptions.Compiled);

                case RDFModelEnums.RDFDatatypes.XSD_BASE64BINARY:
                    try
                    {
                        Convert.FromBase64String(typedLiteral.Value);
                        return true;
                    }
                    catch { return false; }

                case RDFModelEnums.RDFDatatypes.XSD_HEXBINARY:
                    return Regex.IsMatch(typedLiteral.Value, @"^([0-9a-fA-F]{2})*$", RegexOptions.Compiled);
                #endregion

                #region BOOLEAN CATEGORY
                case RDFModelEnums.RDFDatatypes.XSD_BOOLEAN:
                    if (bool.TryParse(typedLiteral.Value, out bool outBool))
                        typedLiteral.Value = outBool ? "true" : "false";
                    else
                    {
                        //Even if lexical space of XSD:BOOLEAN allows 1/0,
                        //it must be converted to true/false value space
                        if (typedLiteral.Value.Equals("1"))
                            typedLiteral.Value = "true";
                        else if (typedLiteral.Value.Equals("0"))
                            typedLiteral.Value = "false";
                        else
                            return false;
                    }
                    return true;
                #endregion

                #region DATETIME CATEGORY
                case RDFModelEnums.RDFDatatypes.XSD_DATETIME:
                    DateTime parsedDateTime;
                    if (DateTime.TryParseExact(typedLiteral.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedDateTime))
                    {
                        typedLiteral.Value = parsedDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedDateTime))
                    {
                        typedLiteral.Value = parsedDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedDateTime))
                    {
                        typedLiteral.Value = parsedDateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedDateTime))
                    {
                        typedLiteral.Value = parsedDateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
                        return true;
                    } 
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_DATE:
                    DateTime parsedDate;
                    if (DateTime.TryParseExact(typedLiteral.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedDate))
                    {
                        typedLiteral.Value = parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "yyyy-MM-ddK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedDate))
                    {
                        typedLiteral.Value = parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_TIME:
                    DateTime parsedTime;
                    if (DateTime.TryParseExact(typedLiteral.Value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedTime))
                    {
                        typedLiteral.Value = parsedTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "HH:mm:ssK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedTime))
                    {
                        typedLiteral.Value = parsedTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedTime))
                    {
                        typedLiteral.Value = parsedTime.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedTime))
                    {
                        typedLiteral.Value = parsedTime.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_GMONTHDAY:
                    DateTime parsedGMonthDay;
                    if (DateTime.TryParseExact(typedLiteral.Value, "--MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGMonthDay))
                    {
                        typedLiteral.Value = parsedGMonthDay.ToString("--MM-dd", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "--MM-ddK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGMonthDay))
                    {
                        typedLiteral.Value = parsedGMonthDay.ToString("--MM-dd", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_GYEARMONTH:
                    DateTime parsedGYearMonth;
                    if (DateTime.TryParseExact(typedLiteral.Value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGYearMonth))
                    {
                        typedLiteral.Value = parsedGYearMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "yyyy-MMK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGYearMonth))
                    {
                        typedLiteral.Value = parsedGYearMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_GYEAR:
                    DateTime parsedGYear;
                    if (DateTime.TryParseExact(typedLiteral.Value, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGYear))
                    {
                        typedLiteral.Value = parsedGYear.ToString("yyyy", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "yyyyK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGYear))
                    {
                        typedLiteral.Value = parsedGYear.ToString("yyyy", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_GMONTH:
                    DateTime parsedGMonth;
                    if (DateTime.TryParseExact(typedLiteral.Value, "MM", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGMonth))
                    {
                        typedLiteral.Value = parsedGMonth.ToString("MM", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "MMK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGMonth))
                    {
                        typedLiteral.Value = parsedGMonth.ToString("MM", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_GDAY:
                    DateTime parsedGDay;
                    if (DateTime.TryParseExact(typedLiteral.Value, "dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGDay))
                    {
                        typedLiteral.Value = parsedGDay.ToString("dd", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else if (DateTime.TryParseExact(typedLiteral.Value, "ddK", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsedGDay))
                    {
                        typedLiteral.Value = parsedGDay.ToString("dd", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;
                #endregion

                #region TIMESPAN CATEGORY
                case RDFModelEnums.RDFDatatypes.XSD_DURATION:
                    try
                    {
                        XmlConvert.ToTimeSpan(typedLiteral.Value);
                        return true;
                    }
                    catch { return false; }
                #endregion

                #region NUMERIC CATEGORY
                case RDFModelEnums.RDFDatatypes.XSD_DECIMAL:
                    if (decimal.TryParse(typedLiteral.Value, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal outDecimal))
                    {
                        typedLiteral.Value = Convert.ToString(outDecimal, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_DOUBLE:
                    if (double.TryParse(typedLiteral.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double outDouble))
                    {
                        typedLiteral.Value = Convert.ToString(outDouble, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_FLOAT:
                    if (float.TryParse(typedLiteral.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float outFloat))
                    {
                        typedLiteral.Value = Convert.ToString(outFloat, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_INTEGER:
                    if (decimal.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal outInteger))
                    {
                        typedLiteral.Value = Convert.ToString(outInteger, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_LONG:
                    if (long.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long outLong))
                    {
                        typedLiteral.Value = Convert.ToString(outLong, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_INT:
                    if (int.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int outInt))
                    {
                        typedLiteral.Value = Convert.ToString(outInt, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_SHORT:
                    if (short.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short outShort))
                    {
                        typedLiteral.Value = Convert.ToString(outShort, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_BYTE:
                    if (sbyte.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte outSByte))
                    {
                        typedLiteral.Value = Convert.ToString(outSByte, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDLONG:
                    if (ulong.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong outULong))
                    {
                        typedLiteral.Value = Convert.ToString(outULong, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDINT:
                    if (uint.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint outUInt))
                    {
                        typedLiteral.Value = Convert.ToString(outUInt, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDSHORT:
                    if (ushort.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort outUShort))
                    {
                        typedLiteral.Value = Convert.ToString(outUShort, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_UNSIGNEDBYTE:
                    if (byte.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte outByte))
                    {
                        typedLiteral.Value = Convert.ToString(outByte, CultureInfo.InvariantCulture);
                        return true;
                    }
                    else
                        return false;

                case RDFModelEnums.RDFDatatypes.XSD_NONPOSITIVEINTEGER:
                    if (decimal.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal outNPInteger))
                    {
                        if (outNPInteger > 0)
                            return false;
                        else
                        {
                            typedLiteral.Value = Convert.ToString(outNPInteger, CultureInfo.InvariantCulture);
                            return true;
                        }
                    }
                    return false;

                case RDFModelEnums.RDFDatatypes.XSD_NEGATIVEINTEGER:
                    if (decimal.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal outNInteger))
                    {
                        if (outNInteger > -1)
                            return false;
                        else
                        {
                            typedLiteral.Value = Convert.ToString(outNInteger, CultureInfo.InvariantCulture);
                            return true;
                        }
                    }
                    return false;

                case RDFModelEnums.RDFDatatypes.XSD_NONNEGATIVEINTEGER:
                    if (decimal.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal outNNInteger))
                    {
                        if (outNNInteger < 0)
                            return false;
                        else
                        {
                            typedLiteral.Value = Convert.ToString(outNNInteger, CultureInfo.InvariantCulture);
                            return true;
                        }
                    }
                    return false;

                case RDFModelEnums.RDFDatatypes.XSD_POSITIVEINTEGER:
                    if (decimal.TryParse(typedLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal outPInteger))
                    {
                        if (outPInteger < 1)
                            return false;
                        else
                        {
                            typedLiteral.Value = Convert.ToString(outPInteger, CultureInfo.InvariantCulture);
                            return true;
                        }
                    }
                    return false;
                #endregion
            }
        }
        #endregion

    }

}