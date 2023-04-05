namespace CSharpCloudJukebox;

public class PropertySet
{
   const string Empty = "";

   const string TypeBool = "bool";
   const string TypeString = "string";
   const string TypeInt = "int";
   const string TypeLong = "long";
   const string TypeUlong = "ulong";

   const string ValueTrue = "true";
   const string ValueFalse = "false";

   private readonly Dictionary<string, PropertyValue> _mapProps;

   public PropertySet()
   {
      _mapProps = new Dictionary<string, PropertyValue>();
   }

   public void Add(string propName, PropertyValue propValue)
   {
      _mapProps[propName] = propValue;
   }

   public void Clear()
   {
      _mapProps.Clear();
   }

   public bool Contains(string propName)
   {
      return _mapProps.ContainsKey(propName);
   }

   public void GetKeys(List<string> keys)
   {
      foreach (KeyValuePair<string, PropertyValue> pair in _mapProps)
      {
         keys.Add(pair.Key);
      }
   }

   private PropertyValue? Get(string propName)
   {
      if (_mapProps.ContainsKey(propName))
      {
         return _mapProps[propName];
      }
      else
      {
         return null;
      }
   }

   public int GetIntValue(string propName)
   {
      PropertyValue? pv = Get(propName);
      if (pv != null && pv.IsInt())
      {
         return pv.GetIntValue();
      }
      else
      {
         return 0;
      }
   }

   public long GetLongValue(string propName)
   {
      PropertyValue? pv = Get(propName);
      if (pv != null && pv.IsLong())
      {
         return pv.GetLongValue();
      }
      else
      {
         return 0L;
      }
   }

   public ulong GetUlongValue(string propName)
   {
      PropertyValue? pv = Get(propName);
      if (pv != null && pv.IsUlong())
      {
         return pv.GetUlongValue();
      }
      else
      {
         return 0L;
      }
   }

   public bool GetBoolValue(string propName)
   {
      PropertyValue? pv = Get(propName);
      if (pv != null && pv.IsBool())
      {
         return pv.GetBoolValue();
      }
      else
      {
         return false;
      }
   }

   public string GetStringValue(string propName)
   {
      PropertyValue? pv = Get(propName);
      if (pv != null && pv.IsString())
      {
         return pv.GetStringValue();
      }
      else
      {
         return Empty;
      }
   }

   public bool WriteToFile(string filePath)
   {
      bool success;
      try
      {
         StreamWriter writer = new StreamWriter(filePath);
         foreach (KeyValuePair<string, PropertyValue> pair in _mapProps)
         {
            string key = pair.Key;
            PropertyValue pv = pair.Value;
            if (pv.IsBool())
            {
               string value = pv.GetBoolValue() ? ValueTrue : ValueFalse;
               writer.Write("{0}|{1}|{2}\n", TypeBool, key, value);
            }
            else if (pv.IsString())
            {
               writer.Write("{0}|{1}|{2}\n", TypeString, key, pv.GetStringValue());
            }
            else if (pv.IsInt())
            {
               writer.Write("{0}|{1}|{2}\n", TypeInt, key, pv.GetIntValue());
            }
            else if (pv.IsLong())
            {
               writer.Write("{0}|{1}|{2}\n", TypeLong, key, pv.GetLongValue());
            }
            else if (pv.IsUlong())
            {
               writer.Write("{0}|{1}|{2}\n", TypeUlong, key, pv.GetUlongValue());
            }
         }
         writer.Flush();
         writer.Close();
         success = true;
      }
      catch (Exception)
      {
         success = false;
      }
      return success;
   }

   public bool ReadFromFile(string filePath)
   {
      bool success = false;
      try
      {
         string fileContents = File.ReadAllText(filePath);
         if (fileContents.Length > 0)
         {
            string[] fileLines = fileContents.Split("\n");
            foreach (var fileLine in fileLines)
            {
               string strippedLine = fileLine.Trim();
               if (strippedLine.Length > 0)
               {
                  string[] fields = strippedLine.Split("|");
                  if (fields.Length == 3)
                  {
                     string dataType = fields[0];
                     string propName = fields[1];
                     string propValue = fields[2];

                     if (dataType.Length > 0 &&
                         propName.Length > 0 &&
                         propValue.Length > 0)
                     {
                        if (dataType == TypeBool)
                        {
                           if (propValue == ValueTrue || propValue == ValueFalse)
                           {
                              bool boolValue = (propValue == ValueTrue);
                              Add(propName, PropertyValue.BoolPropertyValue(boolValue));
                           }
                           else
                           {
                              Console.WriteLine("error: invalid value for type bool '{0}'", dataType);
                              Console.WriteLine("skipping");
                           }
                        }
                        else if (dataType == TypeString)
                        { 
                           Add(propName, PropertyValue.StringPropertyValue(propValue));
                        }
                        else if (dataType == TypeInt)
                        {
                           int intValue = Int32.Parse(propValue);
                           Add(propName, PropertyValue.IntPropertyValue(intValue));
                        }
                        else if (dataType == TypeLong)
                        {
                           long longValue = long.Parse(propValue);
                           Add(propName, PropertyValue.LongPropertyValue(longValue));
                        }
                        else if (dataType == TypeUlong)
                        {
                           ulong ulValue = ulong.Parse(propValue);
                           Add(propName, PropertyValue.UlongPropertyValue(ulValue));
                        }
                        else
                        {
                           Console.WriteLine("error: unrecognized data type '{0}', skipping", dataType);
                        }
                     }
                  }
               }
            }
            success = true;
         }
      }
      catch (Exception)
      {
         success = false;
      }
      return success;
   }

   public int Count()
   {
      return _mapProps.Count;
   }

   public override string ToString()
   {
      bool firstProp = true;
      string propsString = "";
      string commaSpace = ", ";

      foreach (KeyValuePair<string, PropertyValue> pair in _mapProps)
      {
         if (!firstProp)
         {
            propsString += commaSpace;
         }

         propsString += pair.Key;

         if (firstProp)
         {
            firstProp = false;
         }
      }

      return propsString;
   }   
}