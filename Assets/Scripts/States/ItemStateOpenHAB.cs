using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes a state of a single openHAB Item
/// This is a reference json pattern of an OpenHAB state
/// [
///  {
///    "type": "string",
///    "name": "string",
///    "label": "string",
///    "category": "string",
///    "tags": [
///      "string"
///    ],
///    "groupNames": [
///      "string"
///    ],
///    "link": "string",
///    "state": "string",
///    "transformedState": "string",
///    "stateDescription": {
///      "minimum": 0,
///      "maximum": 0,
///      "step": 0,
///      "pattern": "string",
///      "readOnly": false,
///      "options": [
///        {
///          "value": "string",
///          "label": "string"
///        }
///      ]
///    }
///  }
///]
/// </summary>
namespace ThesisHololens.States
{
    [Serializable]
    public class ItemStateOpenHAB : IEquatable<ItemStateOpenHAB>
    {
        public string type;
        public string name;
        public string label;
        public string category;
        public List<string> tags;
        public List<string> groupNames;
        public string link;
        public string state;
        public string transformedState;
        public StateDescription stateDescription;

        [Serializable]
        public class StateDescription
        {
            public int minimum;
            public int maximum;
            public int step;
            public string pattern;
            public bool readOnly;
            public List<Option> options;

            [Serializable]
            public class Option
            {
                public string value;
                public string label;
            }
        }

        /// <summary>
        /// Two items are the same, if their name is the same
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ItemStateOpenHAB other)
        {

            return name.Equals(other.name);

        }
    }
}
