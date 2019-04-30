using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class is made for the Json File
/// </summary>
public class AppData 
{
    //Constants for parsing the json file

    //visual representation
    public const string UIFunction_Switch = "Switch";
    public const string UIFunction_Toggle = "Toggle";

    public const string UIFunction_Slider = "Slider";
    public const string UIFunction_Contact = "Contact";
    public const string UIFunction_Text = "Text";
    public const string UIFunction_Color = "Color";
    public const string UIFunction_Rollershutter = "Rollershutter";
    public const string UIFunction_Player = "Player";

    //open hab representation
    //one open hab representation can have many visual representations
    public static readonly string[] Color =             new string[] { UIFunction_Color};
    public static readonly string[] Contact =           new string[] { UIFunction_Contact, UIFunction_Text };
    public static readonly string[] DateTime =          new string[] { UIFunction_Text };
    public static readonly string[] Dimmer =            new string[] { UIFunction_Text, UIFunction_Slider };
    public static readonly string[] Group =             new string[] { };
    public static readonly string[] Image =             new string[] { };
    public static readonly string[] Location =          new string[] { };
    public static readonly string[] Number =            new string[] { UIFunction_Text };
    public static readonly string[] Number_dimension =  new string[] { };
    public static readonly string[] Player =            new string[] { UIFunction_Player };
    public static readonly string[] Rollershutter =     new string[] { UIFunction_Rollershutter };
    public static readonly string[] String =            new string[] { UIFunction_Text };
    public static readonly string[] Switch =            new string[] { UIFunction_Switch, UIFunction_Toggle, UIFunction_Text };

    public static readonly Dictionary<string, string[]> function_UIContainer_to_realFunction = new Dictionary<string, string[]>() {
        { "Color", Color },
        { "Contact", Contact },
        { "DateTime", DateTime },
        { "Dimmer", Dimmer },
        { "Group", Group },
        { "Image", Image },
        { "Location", Location },
        { "Number", Number },
        { "Number_dimension", Number_dimension },
        { "Player", Player },
        { "Rollershutter", Rollershutter },
        { "String", String },
        { "Switch", Switch }
    };

    public static string generateGuid()
    {
        //totest
        return System.Guid.NewGuid().ToString();
    }




    public List<uniqueIDDevice> MqttDeviceData;


    [System.Serializable]
    public class uniqueIDDevice : ICloneable,IEquatable<uniqueIDDevice>
    {
        //basicly the name
        //we define that as a guid
        public string baseAdress;
       

        //can be something like a lamp, box, sphere
        public string DeviceType3D;

        //the ofsets of the UiContainerBar to the object
        public float HoverOffsetZ;


        //position, scale, rotation of the Device in space relative to theLinked Anchor point
        public float posX;
        public float posY;
        public float posZ;

        public float scaleX;
        public float scaleY;
        public float scaleZ;

        public float rotX;
        public float rotY;
        public float rotZ;

        //Linked anchor
        public string linkedAnchor;


        
        public List<UIContainerData> myUIContainerData;


        //now this can be cloned
        public object Clone()
        {
            uniqueIDDevice device = (uniqueIDDevice) this.MemberwiseClone();

            device.myUIContainerData = new List<UIContainerData>();

            foreach(UIContainerData container in this.myUIContainerData)
            {
                device.myUIContainerData.Add((UIContainerData) container.Clone());
            }

            return device;
        }

        /// <summary>
        /// this guarantees, that all propertys of uniqueIDDevice are the same, but the UIContainerData
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(uniqueIDDevice other)
        {
            if (baseAdress == other.baseAdress &&
                DeviceType3D == other.DeviceType3D &&
                HoverOffsetZ == other.HoverOffsetZ &&
                posX == other.posX &&
                posY == other.posY &&
                posZ == other.posZ &&
                scaleX == other.scaleX &&
                scaleY == other.scaleY &&
                scaleZ == other.scaleZ &&
                rotX == other.rotX &&
                rotY == other.rotY &&
                rotZ == other.rotZ &&
                linkedAnchor == other.linkedAnchor)
                return true;
            else
                return false;
        }

        [System.Serializable]
        public class UIContainerData : ICloneable, IEquatable<UIContainerData>
        {
            public string adress;
            public string displayText;

            //the purpose of a UI container
            //can be one of the Items from Here:
            //https://docs.openhab.org/concepts/items.html
            //use the list at the beginning of the class for Which UIItem can be what function
            public string function_UIContainer;

            //position of this UIElement relative to the Device
            public float posX;
            public float posY;
            public float posZ;//should be 0, but reserved for further use

            public float scaleX;
            public float scaleY;
            public float scaleZ;

            //is the data from this container read only?
            //am i alowed to send commands?
            public bool readOnly;

            //reserved for special properties in a container element
            /// <summary>
            /// For Buttons:
            /// the Icon name
            /// </summary>
            public string specificContainerData1;
            public string specificContainerData2;
            public string specificContainerData3;
            public string specificContainerData4;
            public string specificContainerData5;

            public object Clone()
            {
                return this.MemberwiseClone();
            }

            public bool Equals(UIContainerData other)
            {
                if (adress == other.adress &&
                    function_UIContainer == other.function_UIContainer &&
                    displayText == other.displayText &&
                    posX == other.posX &&
                    posY == other.posY &&
                    scaleX == other.scaleX &&
                    scaleY == other.scaleY &&
                    scaleZ == other.scaleZ &&
                    readOnly == other.readOnly &&
                    specificContainerData1 == other.specificContainerData1 &&
                    specificContainerData2 == other.specificContainerData2 &&
                    specificContainerData3 == other.specificContainerData3 &&
                    specificContainerData4 == other.specificContainerData4 &&
                    specificContainerData5 == other.specificContainerData5)
                    return true;
                else
                    return false;
            }
        }
    }

}
