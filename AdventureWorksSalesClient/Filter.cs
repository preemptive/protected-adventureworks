using System;

namespace AdventureWorksSalesClient
{
    /// <summary> Specifies the filter applied to a query of person records. </summary>
    public class Filter
    {
        /// <summary> The string that is specified by the query's $filter operator. </summary>
        public string FilterString
        {
            get
            {
                var toReturn = "(PersonType eq 'IN')";
                toReturn += GetIdFilterClause("BusinessEntityID", IdValue);
                toReturn += GetNameFilterClause("LastName", LastNameValue);
                toReturn += GetNameFilterClause("FirstName", FirstNameValue);
                return toReturn;
            }
        }

        private string GetIdFilterClause(string fieldName, string operand)
        {
            if (string.IsNullOrWhiteSpace(operand))
            {
                return "";
            }
            var operandInt = int.Parse(operand);
            return $"and ({fieldName} eq {operandInt})";
        }

        private string GetNameFilterClause(string fieldName, string operand)
        {
            if (string.IsNullOrWhiteSpace(operand))
            {
                return "";
            }
            return $"and (startswith(tolower({fieldName}),'{operand.ToLowerInvariant()}') eq true)";
        }
        
        /// <summary>
        /// The customer ID that records must exactly match.
        /// If empty, do not filter the query by customer ID.
        /// </summary>
        public string IdValue { get; set; }

        /// <summary>
        /// The customer last name that records must start with.
        /// Case insensitive. If empty, do not filter the query by last name.
        /// </summary>
        public string LastNameValue { get; set; }
        
        /// <summary>
        /// The customer first name that records must start with.
        /// Case insensitive. If empty, do not filter the query by first name.
        /// </summary>
        public string FirstNameValue { get; set; }
    }
}
