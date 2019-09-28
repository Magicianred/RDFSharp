﻿/*
   Copyright 2012-2019 Marco De Salvo

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
using System.Data;
using System.Linq;
using RDFSharp.Model;

namespace RDFSharp.Query
{

    /// <summary>
    /// RDFValues represents a binding of variables provided directly inside a SPARQL query.
    /// </summary>
    public class RDFValues : RDFPatternGroupMember
    {

        #region Properties
        /// <summary>
        /// Dictionary of bindings representing the SPARQL values
        /// </summary>
        internal Dictionary<String, List<RDFPatternMember>> Bindings { get; set; }
        
        /// <summary>
        /// Represents the current max length of the bindings
        /// </summary>
        internal Int32 MaxBindingsLength
        {
            get
            {
                return this.Bindings?.Select(x => x.Value.Count).Max() ?? 0;
            }
        }
        #endregion

        #region Ctors
        /// <summary>
        /// Default-ctor to build an empty SPARQL values
        /// </summary>
        public RDFValues()
        {
            this.Bindings = new Dictionary<String, List<RDFPatternMember>>();
            this.IsEvaluable = false;
        }
        #endregion

        #region Interfaces
        /// <summary>
        /// Gives the string representation of the SPARQL values
        /// </summary>
        public override String ToString()
        {
            return this.ToString(new List<RDFNamespace>());
        }
        internal String ToString(List<RDFNamespace> prefixes)
        {
            return RDFQueryPrinter.PrintValues(this, prefixes);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds the given binding values to the given variable
        /// </summary>
        public RDFValues AddBindings(RDFVariable variable, List<RDFPatternMember> bindings)
        {
            if (variable != null)
            {

                //Initialize bindings of the given variable
                if (!this.Bindings.ContainsKey(variable.ToString()))
                    this.Bindings.Add(variable.ToString(), new List<RDFPatternMember>());

                //Populate bindings of the given variable
                //(null indicates the special UNDEF binding)
                if (bindings?.Any() ?? false)
                    bindings.ForEach(b => this.Bindings[variable.ToString()].Add((b is RDFResource || b is RDFLiteral) ? b : null));
                else
                    this.Bindings[variable.ToString()].Add(null);

                //Mark the SPARQL values as evaluable
                this.IsEvaluable = true;

            }
            return this;
        }
        
        /// <summary>
        /// Gets the datatable representing the SPARQL values
        /// </summary>
        internal DataTable GetDataTable()
        {
            DataTable result = new DataTable(this.ToString());

            //Create the columns of the SPARQL values
            this.Bindings.ToList()
                         .ForEach(b => RDFQueryEngine.AddColumn(result, b.Key));
            result.AcceptChanges();

            //Create the rows of the SPARQL values
            result.BeginLoadData();
            for (int i = 0; i < this.MaxBindingsLength; i++)
            {
                Dictionary<String, String> bindings = new Dictionary<String, String>();
                this.Bindings.ToList()
                             .ForEach(b => bindings.Add(b.Key, b.Value.ElementAtOrDefault(i)?.ToString()));
                RDFQueryEngine.AddRow(result, bindings);
            }
            result.EndLoadData();

            return result;
        }
        #endregion

    }

}