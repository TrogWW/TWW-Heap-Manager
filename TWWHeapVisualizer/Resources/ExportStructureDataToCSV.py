# Import the necessary Ghidra modules
from ghidra.program.model.data import DataType, Structure, Enum, Pointer, Array, FunctionDefinition, TypeDef
import csv

# Define location for where to output csv files
output_directory = 'C:\\Output\\'
# Define a set to store unknown data types
unknown_data_types = set()

def get_data_type_class(data_type):
    if isinstance(data_type, Structure):
        return 'Structure'
    elif isinstance(data_type, Enum):
        return 'Enum'
    elif isinstance(data_type, Pointer):
        return 'Pointer'
    elif isinstance(data_type, Array):
        return 'Array'
    elif isinstance(data_type, FunctionDefinition):
        return 'FunctionDefinition'
    elif isinstance(data_type, TypeDef):
        return 'TypeDef'
    else:
        return 'Unknown'

# Define the function to export data types to CSV
def export_data_types_to_csv():
    # Specify the path for the CSV files

    
    # Create CSV files for each data type class
    data_type_classes = {
        Structure: 'structure_types.csv',
        Enum: 'enum_types.csv',
        Pointer: 'pointer_types.csv',
        Array: 'array_types.csv',
        FunctionDefinition: 'function_types.csv',
        TypeDef: 'typedef_types.csv'
    }
    
    # Iterate through data type classes
    for data_type_class, filename in data_type_classes.items():
        csv_file_path = output_directory + filename
        
        try:
            # Open the CSV file in write mode with 'wb'
            with open(csv_file_path, 'wb') as csvfile:
                # Define the CSV writer
                csv_writer = csv.writer(csvfile)
                
                # Write the header row
                if data_type_class == Structure:
                    csv_writer.writerow(['StructureName', 'Offset', 'Length', 'Data Type', 'DataStructType', 'Name' ])
                elif data_type_class == Pointer:
                    csv_writer.writerow(['Pointer Name', 'Target Data Type', 'DataStructType'])
                elif data_type_class == Array:
                    csv_writer.writerow(['Array Name', 'Component Data Type', 'DataStructType', 'Number of Elements'])
                elif data_type_class == FunctionDefinition:
                    csv_writer.writerow(['Function Name', 'Return Type', 'Parameter Types'])
                elif data_type_class == TypeDef:
                    csv_writer.writerow(['Type Definition Name', 'Base Data Type', 'DataStructType'])
                
                # If the data type is Enum, open a separate CSV file for enums
                if data_type_class == Enum:
                    enum_csv_file_path = output_directory + filename
                    with open(enum_csv_file_path, 'wb') as enum_csvfile:
                        enum_csv_writer = csv.writer(enum_csvfile)
    
                        enum_csv_writer.writerow(['Enum Name', 'Value', 'Entry Name'])
                        # Iterate through all data types
                        for dataType in currentProgram.getDataTypeManager().getAllDataTypes():
                            # Check if the data type matches the current class
                            if isinstance(dataType, Enum):
                                # Get the name of the enum
                                enum_data_type_name = dataType.getName()
                                # Iterate through enumeration values
                                enum_names = dataType.getNames()
                                for i in range(0, len(enum_names)):
                                    # Get the value of the enumeration constant
                                    #enum_constant = enum_values[i]
                                    enum_name = enum_names[i]
                                    enum_constant = dataType.getValue(enum_name)
                                    # Write the data to the enum CSV file
                                    enum_csv_writer.writerow([enum_data_type_name, enum_constant, enum_name])
                
                # For other data types, continue writing to their respective CSV files
                else:
                    # Iterate through all data types
                    for dataType in currentProgram.getDataTypeManager().getAllDataTypes():
                        # Check if the data type matches the current class
                        if isinstance(dataType, data_type_class):
                            # Write data based on the data type class
                            if data_type_class == Structure:
                                # Get the total size of the structure
                                total_size = dataType.getLength()
                                # Check if the structure has no defined components but a defined size
                                if dataType.getNumComponents() == 0 and total_size > 0:
                                    # Iterate over each byte in the structure                                  
                                    for offset in range(total_size):
                                        # Output each byte as a separate row in the CSV file
                                        csv_writer.writerow([dataType.getName().replace(',', ''), offset, 1, 'byte', 'Unknown', 'field_0x{:X}'.format(offset)])
                                else:
                                    for component in dataType.getComponents():
                                        name = component.getFieldName()
                                        if name is None:
                                            name = 'field_0x{:X}'.format(component.getOffset())
                                        offset = component.getOffset()
                                        length = component.getLength()
                                        dataTypeName = component.getDataType().getDisplayName()
                                        dataStructType = get_data_type_class(component.getDataType())
                                        if dataStructType == 'Unknown' and dataTypeName not in unknown_data_types:
                                            unknown_data_types.add(dataTypeName)
                                        csv_writer.writerow([dataType.getName(), offset, length, dataTypeName, dataStructType, name])
                                    
                            elif data_type_class == Pointer:
                                pointer_name = dataType.getName()
                                target_data_type = dataType.getDataType()
                                target_data_type_name = target_data_type.getDisplayName() if target_data_type else ''
                                dataStructType = get_data_type_class(dataType.getDataType())
                                if dataStructType == 'Unknown' and target_data_type_name not in unknown_data_types:
                                    unknown_data_types.add(target_data_type_name)
                                csv_writer.writerow([pointer_name, target_data_type_name,dataStructType])
                                    
                            elif data_type_class == Array:
                                array_name = dataType.getName()
                                component_data_type = dataType.getDataType().getDisplayName()
                                dataStructType = get_data_type_class(dataType.getDataType())
                                num_elements = dataType.getNumElements()
                                if dataStructType == 'Unknown' and component_data_type not in unknown_data_types:
                                        unknown_data_types.add(component_data_type)
                                csv_writer.writerow([array_name, component_data_type, dataStructType, num_elements])
                                    
                            elif data_type_class == FunctionDefinition:
                                function_name = dataType.getName()
                                return_type = dataType.getReturnType().getDisplayName()
                                param_types = ', '.join(param.getDataType().getDisplayName() for param in dataType.getArguments())
                                csv_writer.writerow([function_name, return_type, param_types])
                                    
                            elif data_type_class == TypeDef:
                                type_def_name = dataType.getName()
                                base_data_type = dataType.getBaseDataType().getDisplayName()
                                dataStructType = get_data_type_class(dataType.getBaseDataType())
                                if dataStructType == 'Unknown' and base_data_type not in unknown_data_types:
                                    unknown_data_types.add(base_data_type)
                                csv_writer.writerow([type_def_name, base_data_type, dataStructType])
                                        
        except Exception as e:
            print("Error:", e)
    
    # Export unknown data types to a new CSV file if it hasn't been added before
    unknown_types_file = output_directory + "unknown_types.csv"
    with open(unknown_types_file, 'w') as unknown_csvfile:
        unknown_csv_writer = csv.writer(unknown_csvfile, lineterminator='\n')
        for unknown_type in unknown_data_types:
            unknown_csv_writer.writerow([unknown_type])


# Run the export_data_types_to_csv function
export_data_types_to_csv()
