using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EZAccess.Data;
public class EZField
{
    #region Private Fields
    private readonly Type _model;
    private readonly string _fieldName;
    private readonly PropertyInfo _fieldInfo;
    private string? _displayName;           //DisplayAttribute.Name
    private string? _displayShortName;      //DisplayAttribute.Name
    private bool? _allowEdit;               //EditableAttribute.AllowEdit
    private string? _dataFormatString;      //DisplayFormatAttribute.DataFormatString
    private string? _nullDisplayText;       //DisplayFormatAttribute.NullDisplayText
    private bool? _key;                     //KeyAttribute
    private bool? _isRequired;              //RequiredAttribute
    private bool? _isCreditCard;            //CreditCardAttribute
    private bool? _isEmailAddress;          //EmailAddressAttribute 
    private bool? _isPhone;                 //PhoneAttribute
    private bool? _hasMaxLength;            //MaxLengthAttribute
    private int? _maxLength;                //MaxLengthAttribute.Length
    private bool? _hasMinLength;            //MinLengthAttribute
    private int? _minLength;                //MixLengthAttribute.Length
    private bool? _hasRange;                //RangeAttribute
    private object? _rangeMaximum;          //RangeAttribute.Maximum
    private object? _rangeMinimum;          //RangeAttribute.Minimum
    private Type? _rangeType;               //RangeAttribute.OperandType
    private bool? _hasRegularExpression;     //RegularExpressionAttribute
    private string? _regularExpression;     //RegularExpressionAttribute.Pattern
    private bool? _scaffoldColumn;          //ScaffoldColumnAttribute
    private bool? _hasStringLength;         //StringLengthAttribute
    private int? _stringMaxLength;          //StringLengthAttribute
    private int? _stringMinLength;          //StringLengthAttribute
    private bool? _isUrl;                   //UrlAttribute
    private bool? _isTimeStamp;             //TimestampAttribute

    #endregion

    #region Constructors
    public EZField(Type model, string fieldName)
    {
        _model = model;
        _fieldName = fieldName;
        var p = _model.GetProperty(_fieldName);

        if (p != null)
        {
            _fieldInfo = p;
        }
        else
        {
            throw new ArgumentException($"The object {nameof(model)} does not have a property with name '{fieldName}'");
        }
    }

    public EZField(Type model, PropertyInfo propertyInfo)
    {
        _model = model;
        _fieldInfo = propertyInfo;
        _fieldName = propertyInfo.Name;
    }

    #endregion

    /// <summary>
    /// Returns the value of the property in the given context.
    /// </summary>
    /// <param name="context">The object of type Model which supplies the values</param>
    /// <returns>The value as an object</returns>
    public object? Value(object? context)
    {
        var value = _fieldInfo.GetValue(context);
        return value;
    }

    /// <summary>
    /// Returns the value of the property in the given context. The value is returned as string
    /// in the supplied DisplayFormat or the default display format.
    /// </summary>
    /// <param name="context">The object of type Model which supplies the values</param>
    /// <param name="dataFormatString">An optional string by which the value is formated</param>
    /// <returns></returns>
    public string ValueAsString(object? context, string? dataFormatString = null)
    {
        var value = _fieldInfo.GetValue(context);
        if (value is null) return NullDisplayText;
        else {
            if (value is IFormattable) return FormatedValue((IFormattable)value, dataFormatString);
            else return value.ToString() ?? NullDisplayText;
        }
    }

    private string FormatedValue(IFormattable value, string? dataFormatString)
    {
        string formatString = dataFormatString is not null ? dataFormatString : DataFormatString;
        string? result = formatString == string.Empty ? value.ToString() : value.ToString(formatString, null);
        return result ?? NullDisplayText;
    }

    #region Public Properties

    /// <summary>
    /// Returns the Type of the model on which the attributes are based
    /// </summary>
    public Type Model => _model;

    /// <summary>
    /// Returns the object PropertyInfo for the current property.
    /// </summary>
    public PropertyInfo FieldInfo => _fieldInfo;

    /// <summary>
    /// Returns the name of the current property
    /// </summary>
    public string FieldName => _fieldName;

    /// <summary>
    /// Get the attribute value of DisplayAttribute.Name
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (_displayName is null)
            {
                DisplayAttribute? attr = (DisplayAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(DisplayAttribute));
                if (attr is not null) _displayName = attr?.Name;
                if (_displayName is null) _displayName = _fieldName;
            }
            return _displayName;
        }
    }

    /// <summary>
    /// Get the attribute value of DisplayAttribute.ShortName
    /// </summary>
    public string DisplayShortName
    {
        get
        {
            if (_displayShortName is null)
            {
                DisplayAttribute? attr = (DisplayAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(DisplayAttribute));
                if (attr is not null) _displayShortName = attr?.ShortName;
                if (_displayShortName is null) _displayShortName = _fieldName;
            }
            return _displayShortName;
        }
    }

    /// <summary>
    /// Get the attribute value of EditableAttribute.AllowEdit
    /// </summary>
    public bool AllowEdit
    {
        get
        {
            if (_allowEdit is null)
            {
                EditableAttribute? attr = (EditableAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(EditableAttribute));
                if (attr is not null) _allowEdit = attr.AllowEdit;
                if (_allowEdit is null) _allowEdit = true;
            }
            return _allowEdit ?? true;
        }
    }

    /// <summary>
    /// Get the attribute value of DisplayFormatAttribute.DataFormatString
    /// </summary>
    public string DataFormatString
    {
        get
        {
            if (_dataFormatString is null)
            {
                DisplayFormatAttribute? attr = (DisplayFormatAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(DisplayFormatAttribute));
                if (attr is not null) _dataFormatString = attr.DataFormatString;
                if (_dataFormatString is null) _dataFormatString = string.Empty;
            }
            return _dataFormatString;
        }
    }

    /// <summary>
    /// Get the attribute value of DisplayFormatAttribute.NullDisplayText
    /// </summary>
    public string NullDisplayText
    {
        get
        {
            if (_nullDisplayText is null)
            {
                DisplayFormatAttribute? attr = (DisplayFormatAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(DisplayFormatAttribute));
                if (attr is not null) _nullDisplayText = attr.NullDisplayText;
                if (_nullDisplayText is null) _nullDisplayText = string.Empty;
            }
            return _nullDisplayText;
        }
    }

    /// <summary>
    /// Get the attribute value of KeyAttribute
    /// </summary>
    public bool Key
    {
        get
        {
            if (_key is null) {
                KeyAttribute? attr = (KeyAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(KeyAttribute));
                if (attr is not null) _key = true;
                else _key = false;
            }
            return _key ?? false;
        }
    }

    /// <summary>
    /// Get the System.Type with which the property is declared
    /// </summary>
    public Type PropertyType
    {
        get
        {
            return _fieldInfo.PropertyType;
        }
    }

    /// <summary>
    /// Get the attribute value of RequiredAttribute
    /// </summary>
    public bool IsRequired
    {
        get
        {
            if (_isRequired is null) {
                RequiredAttribute? attr = (RequiredAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(RequiredAttribute));
                if (attr is not null) _isRequired = true;
                if (_isRequired is null) _isRequired = false;
            }
            return _isRequired ?? false;
        }
    }

    /// <summary>
    /// Get the attribute value of CreditCardAttribute
    /// </summary>
    public bool IsCreditCard
    {
        get
        {
            if (_isCreditCard is null)
            {
                CreditCardAttribute? attr = (CreditCardAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(CreditCardAttribute));
                if (attr is not null) _isCreditCard = true;
                if (_isCreditCard is null) _isCreditCard = false;
            }
            return _isCreditCard ?? false;
        }
    }

    /// <summary>
    /// Get the attribute value of EmailAddressAttribute
    /// </summary>
    public bool IsEmailAddress
    {
        get
        {
            if (_isEmailAddress is null)
            {
                EmailAddressAttribute? attr = (EmailAddressAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(EmailAddressAttribute));
                if (attr is not null) _isEmailAddress = true;
                if (_isEmailAddress is null) _isEmailAddress = false;
            }
            return _isEmailAddress ?? false;
        }
    }

    /// <summary>
    /// Get the attribute value of PhoneAttribute
    /// </summary>
    public bool IsPhone
    {
        get
        {
            if (_isPhone is null)
            {
                PhoneAttribute? attr = (PhoneAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(PhoneAttribute));
                if (attr is not null) _isPhone = true;
                if (_isPhone is null) _isPhone = false;
            }
            return _isPhone ?? false;
        }
    }

    /// <summary>
    /// Get the attribute value of MaxLengthAttribute.Length
    /// </summary>
    public int? MaxLength
    {
        get
        {
            if (_hasMaxLength is null)
            {
                MaxLengthAttribute? attr = (MaxLengthAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(MaxLengthAttribute));
                if (attr is not null)
                {
                    _hasMaxLength = true;
                    _maxLength = attr.Length;
                }
                else
                {
                    _hasMaxLength = false;
                    _maxLength = null;
                }
            }
            return _maxLength;
        }
    }

    /// <summary>
    /// Get the attribute value of MinLengthAttribute.Length
    /// </summary>
    public int? MinLength
    {
        get
        {
            if (_hasMinLength is null)
            {
                MinLengthAttribute? attr = (MinLengthAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(MinLengthAttribute));
                if (attr is not null)
                {
                    _hasMinLength = true;
                    _minLength = attr.Length;
                }
                else
                {
                    _hasMinLength = false;
                    _minLength = null;
                }
            }
            return _minLength;
        }
    }

    /// <summary>
    /// Get the attribute value of RangeAttribute.Maximum
    /// </summary>
    public object? RangeMaximum
    {
        get
        {
            if (HasRange) return _rangeMaximum;
            else return null;
        }
    }

    /// <summary>
    /// Get the attribute value of RangeAttribute.Minimum
    /// </summary>
    public object? RangeMinimum
    {
        get
        {
            if (HasRange) return _rangeMinimum;
            else return null;
        }
    }

    /// <summary>
    /// Get the attribute value of RangeAttribute.OperandType
    /// </summary>
    public Type? RangeType
    {
        get
        {
            if (HasRange) return _rangeType;
            else return null;
        }
    }

    /// <summary>
    /// Get the attribute value of RangeAttribute returns whether there is an attribute
    /// </summary>
    public bool HasRange 
    {
        get
        {
            if (_hasRange is null)
            {
                RangeAttribute? attr = (RangeAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(RangeAttribute));
                if (attr is not null)
                {
                    _hasRange = true;
                    _rangeMaximum = attr.Maximum;
                    _rangeMinimum = attr.Minimum;
                    _rangeType = attr.OperandType;
                }
                else _hasRange = false;
            }
            return _hasRange ?? false;
        }
    }

    /// <summary>
    /// Get the attribute value of RegularExpressionAttribute.Pattern
    /// </summary>
    public string? RegularExpression {
        get
        {
            if (_hasRegularExpression is null)
            {
                RegularExpressionAttribute? attr = (RegularExpressionAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(RegularExpressionAttribute));
                if (attr is not null)
                {
                    _hasRegularExpression = true;
                    _regularExpression = attr.Pattern;
                }
                else _hasRegularExpression = false;
            }
            return _regularExpression;
        }
    }

    /// <summary>
    /// Get the attribute value of ScaffoldColumnAttribute.Scaffold
    /// </summary>
    public bool ScaffoldColumn
    {
        get
        {
            if (_scaffoldColumn is null)
            {
                ScaffoldColumnAttribute? attr = (ScaffoldColumnAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(ScaffoldColumnAttribute));
                if (attr is not null) _scaffoldColumn = attr.Scaffold;
                if (_scaffoldColumn is null) _scaffoldColumn = true;
            }
            return _scaffoldColumn ?? true;
        }
    }

    /// <summary>
    /// Get the attribute value of StringLengthAttribute.MaximumLength
    /// </summary>
    public int? StringMaxLength
    {
        get
        {
            if (HasStringLength) return _stringMaxLength;
            else return null;
        }
    }

    /// <summary>
    /// Get the attribute value of StringLengthAttribute.MinimumLength
    /// </summary>
    public int? StringMinLength
    {
        get
        {
            if (HasStringLength) return _stringMinLength;
            else return null;
        }
    }

    /// <summary>
    /// Get the attribute value of StringLengthAttribute and returns whether there is an attribute
    /// </summary>
    private bool HasStringLength
    {
        get
        {
            if (_hasStringLength is null)
            {
                StringLengthAttribute? attr = (StringLengthAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(StringLengthAttribute));
                if (attr is not null)
                {
                    _stringMaxLength = attr.MaximumLength;
                    _stringMinLength = attr.MinimumLength;
                    _hasStringLength = true;
                }
                else _hasStringLength = false;
            }
            return _hasStringLength ?? false;
        }
    }

    /// <summary>
    /// Get the attribute value of UrlAttribute
    /// </summary>
    public bool IsUrl 
    {
        get 
        {
            if (_isUrl is null)
            {
                UrlAttribute? attr = (UrlAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(UrlAttribute));
                if (attr is not null) _isUrl = true;
                else _isUrl = false;
            }
            return _isUrl ?? false;
        }
    }

    /// <summary>
    /// Get the attribute value of TimestampAttribute
    /// </summary>
    public bool IsTimeStamp { 
        get 
        { 
            if (_isTimeStamp is null)
            {
                TimestampAttribute? attr = (TimestampAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(TimestampAttribute));
                if (attr is not null) _isTimeStamp = true;
                else _isTimeStamp = false;
            }
            return _isTimeStamp ?? false;
        }
    }

    #endregion
}

//AssociatedMetadataTypeTypeDescriptionProvider
//AssociationAttribute
//CompareAttribute
//ConcurrencyCheckAttribute
//CustomValidationAttribute
//EnumDataTypeAttribute
//FileExtensionsAttribute
//FilterUIHintAttribute
//MetadataTypeAttribute
//TimestampAttribute
//UIHintAttribute
//ValidationAttribute
//ValidationContext
//ValidationException
//ValidationResult
//Validator
