namespace CSharpCloudJukebox;

public class PropertyValue
{
   private const char TypeBool = 'b';
   private const char TypeString = 's';
   private const char TypeInt = 'i';
   private const char TypeLong = 'l';
   private const char TypeUlong = 'u';
   
   private readonly char _dataType;
   private bool _boolValue;
   private string _stringValue;
   private int _intValue;
   private long _longValue;
   private ulong _ulongValue;

   public static PropertyValue BoolPropertyValue(bool value)
   {
      PropertyValue pv = new PropertyValue(TypeBool);
      pv._boolValue = value;
      return pv;
   }

   public static PropertyValue StringPropertyValue(String value)
   {
      PropertyValue pv = new PropertyValue(TypeString);
      pv._stringValue = value;
      return pv;
   }

   public static PropertyValue IntPropertyValue(int value)
   {
      PropertyValue pv = new PropertyValue(TypeInt);
      pv._intValue = value;
      return pv;
   }

   public static PropertyValue LongPropertyValue(long value)
   {
      PropertyValue pv = new PropertyValue(TypeLong);
      pv._longValue = value;
      return pv;
   }

   public static PropertyValue UlongPropertyValue(ulong value)
   {
      PropertyValue pv = new PropertyValue(TypeUlong);
      pv._ulongValue = value;
      return pv;
   }

   public bool IsBool()
   {
      return _dataType == TypeBool;
   }
   
   public bool IsString()
   {
      return _dataType == TypeString;
   }
   
   public bool IsInt()
   {
      return _dataType == TypeInt;
   }

   public bool IsLong()
   {
      return _dataType == TypeLong;
   }

   public bool IsUlong()
   {
      return _dataType == TypeUlong;
   }

   public bool GetBoolValue()
   {
      return _boolValue;
   }

   public string GetStringValue()
   {
      return _stringValue;
   }

   public int GetIntValue()
   {
      return _intValue;
   }

   public long GetLongValue()
   {
      return _longValue;
   }

   public ulong GetUlongValue()
   {
      return _ulongValue;
   }

   private PropertyValue(char dataType)
   {
      _dataType = dataType;
      _boolValue = false;
      _stringValue = "";
      _intValue = 0;
      _longValue = 0;
      _ulongValue = 0;
   }
}