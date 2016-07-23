﻿/*
   Copyright 2012-2016 Marco De Salvo

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
using System.IO;

namespace RDFSharp.Model
{

    /// <summary>
    /// RDFGraphSerializer exposes choices to read and write RDF graph data in supported formats.
    /// </summary>
    public static class RDFGraphSerializer {

        #region Methods

        #region Write
        /// <summary>
        /// Writes the given graph to the given file in the given RDF format. 
        /// </summary>
        public static void WriteRDF(RDFModelEnums.RDFFormats rdfFormat, RDFGraph graph, String filepath) {
            if (graph        != null) {
                if (filepath != null) {
                    switch(rdfFormat) {
                        case RDFModelEnums.RDFFormats.NTriples:
                             RDFNTriples.Serialize(graph, filepath);
                             break;
                        case RDFModelEnums.RDFFormats.RdfXml:
                             RDFXml.Serialize(graph, filepath);
                             break;
                        case RDFModelEnums.RDFFormats.TriX:
                             RDFTriX.Serialize(graph, filepath);
                             break;
                        case RDFModelEnums.RDFFormats.Turtle:
                             RDFTurtle.Serialize(graph, filepath);
                             break;
                    }
                }
                else {
                    throw new RDFModelException("Cannot write RDF file because given \"filepath\" parameter is null.");
                }
            }
            else {
                throw new RDFModelException("Cannot write RDF file because given \"graph\" parameter is null.");
            }
        }

        /// <summary>
        /// Writes the given graph to the given stream in the given RDF format. 
        /// </summary>
        public static void WriteRDF(RDFModelEnums.RDFFormats rdfFormat, RDFGraph graph, Stream outputStream) {
            if (graph            != null) {
                if (outputStream != null) {
                    switch    (rdfFormat) {
                        case RDFModelEnums.RDFFormats.NTriples:
                             RDFNTriples.Serialize(graph, outputStream);
                             break;
                        case RDFModelEnums.RDFFormats.RdfXml:
                             RDFXml.Serialize(graph, outputStream);
                             break;
                        case RDFModelEnums.RDFFormats.TriX:
                             RDFTriX.Serialize(graph, outputStream);
                             break;
                        case RDFModelEnums.RDFFormats.Turtle:
                             RDFTurtle.Serialize(graph, outputStream);
                             break;
                    }
                }
                else {
                    throw new RDFModelException("Cannot write RDF file because given \"outputStream\" parameter is null.");
                }
            }
            else {
                throw new RDFModelException("Cannot write RDF file because given \"graph\" parameter is null.");
            }
        }
        #endregion

        #region Read
        /// <summary>
        /// Reads the given file in the given RDF format to a graph. 
        /// </summary>
        public static RDFGraph ReadRDF(RDFModelEnums.RDFFormats rdfFormat, String filepath) {
            if (filepath     != null) {
                if (File.Exists(filepath)) {
                    switch     (rdfFormat) {
                        case RDFModelEnums.RDFFormats.NTriples:
                             return RDFNTriples.Deserialize(filepath);
                        case RDFModelEnums.RDFFormats.RdfXml:
                             return RDFXml.Deserialize(filepath);
                        case RDFModelEnums.RDFFormats.TriX:
                             return RDFTriX.Deserialize(filepath);
                        case RDFModelEnums.RDFFormats.Turtle:
                             throw new RDFModelException("Cannot read RDF file because reading of Turtle format is not supported. What about joining the project to contribute it?");
                    }
                }
                throw new RDFModelException("Cannot read RDF file because given \"filepath\" parameter (" + filepath + ") does not indicate an existing file.");
            }
            throw new RDFModelException("Cannot read RDF file because given \"filepath\" parameter is null.");
        }

        /// <summary>
        /// Reads the given stream in the given RDF format to a graph. 
        /// </summary>
        public static RDFGraph ReadRDF(RDFModelEnums.RDFFormats rdfFormat, Stream inputStream) {
            if (inputStream != null) {
                switch   (rdfFormat) {
                    case RDFModelEnums.RDFFormats.NTriples:
                         return RDFNTriples.Deserialize(inputStream);
                    case RDFModelEnums.RDFFormats.RdfXml:
                         return RDFXml.Deserialize(inputStream);
                    case RDFModelEnums.RDFFormats.TriX:
                         return RDFTriX.Deserialize(inputStream);
                    case RDFModelEnums.RDFFormats.Turtle:
                         throw new RDFModelException("Cannot read RDF stream because reading of Turtle format is not supported. What about joining the project to contribute it?");
                }
            }
            throw new RDFModelException("Cannot read RDF stream because given \"inputStream\" parameter is null.");
        }
        #endregion

        #endregion

    }

}