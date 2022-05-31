using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EZAccess.Data;
public class EZField
{
    private readonly Type _model;
    private readonly string _fieldName;
    private readonly PropertyInfo _fieldInfo;

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

    public Type Model => _model;
    public PropertyInfo FieldInfo => _fieldInfo;
    public string FieldName => _fieldName;
    public string Display
    {
        get
        {
            DisplayAttribute? attr = (DisplayAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(DisplayAttribute));
            if (attr != null)
            {
                return attr.Name ?? _fieldName;
            }
            return _fieldName;
        }
    }

    public bool Editable
    {
        get
        {
            EditableAttribute? attr = (EditableAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(EditableAttribute));
            if (attr != null)
            {
                return attr.AllowEdit;
            }
            return true;
        }
    }

    public string? DisplayFormat
    {
        get
        {
            DisplayFormatAttribute? attr = (DisplayFormatAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(DisplayFormatAttribute));
            if (attr != null)
            {
                return attr.DataFormatString;
            }
            return null;
        }
    }

    public bool Key
    {
        get
        {
            KeyAttribute? attr = (KeyAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(KeyAttribute));
            if (attr != null)
            {
                return true;
            }
            return false;
        }
    }

    public DataType? DataType
    {
        get
        {
            DataTypeAttribute? attr = (DataTypeAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(DataTypeAttribute));
            if (attr != null)
            {
                return attr.DataType;
            }
            return null;
        }
    }
    public bool Required
    {
        get
        {
            RequiredAttribute? attr = (RequiredAttribute?)Attribute.GetCustomAttribute(_fieldInfo, typeof(RequiredAttribute));
            if (attr != null)
            {
                return true;
            }
            return false;
        }
    }
}

//AssociatedMetadataTypeTypeDescriptionProvider
//AssociationAttribute
//CompareAttribute
//ConcurrencyCheckAttribute
//CreditCardAttribute
//CustomValidationAttribute
//EmailAddressAttribute
//EnumDataTypeAttribute
//FileExtensionsAttribute
//FilterUIHintAttribute
//MaxLengthAttribute
//MetadataTypeAttribute
//MinLengthAttribute
//PhoneAttribute
//RangeAttribute
//RegularExpressionAttribute
//ScaffoldColumnAttribute
//StringLengthAttribute
//TimestampAttribute
//UIHintAttribute
//UrlAttribute
//ValidationAttribute
//ValidationContext
//ValidationException
//ValidationResult
//Validator
