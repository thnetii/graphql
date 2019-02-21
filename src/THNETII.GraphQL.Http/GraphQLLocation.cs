using System;
using System.Collections.Generic;

namespace THNETII.GraphQL.Http
{
    /// <summary>
	/// Represents the location where the <see cref="GraphQLError"/> has been found
	/// </summary>
	public class GraphQLLocation : IEquatable<GraphQLLocation>
    {

        /// <summary>
        /// The Column
        /// </summary>
        public uint Column { get; set; }

        /// <summary>
        /// The Line
        /// </summary>
        public uint Line { get; set; }

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as GraphQLLocation);

        /// <inheritdoc />
        public bool Equals(GraphQLLocation other)
        {
            switch (other)
            {
                case null:
                    return false;
                case GraphQLLocation l when ReferenceEquals(this, l):
                    return true;
                case GraphQLLocation l when Column != l.Column:
                    return false;
                case GraphQLLocation l when Line != l.Line:
                    return false;
            }
            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode() => EqualityComparer<GraphQLLocation>.Default.GetHashCode(this);

        /// <inheritdoc />
        public static bool operator ==(GraphQLLocation location1, GraphQLLocation location2)
        {
            if (location1 is null)
                return location2 is null ? true : false;
            return location1.Equals(location2);
        }

        /// <inheritdoc />
        public static bool operator !=(GraphQLLocation location1, GraphQLLocation location2)
        {
            if (location1 is null)
                return location2 is null ? false : true;
            return !location1.Equals(location2);
        }
    }
}
