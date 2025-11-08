# ODDB

## Overview
ODDB is a semi-structured open-source database solution available for Unity.
It aims to make data easily accessible and usable by binding Tables created through the table editor to specific classes.
The generated table data can be easily synchronized with external sources such as CSV and Google Sheets, making it convenient for collaboration among various roles.

## Documentation
- 📘 [한국어](./Doc/README_KR.md)

## Table of Contents
- [Key Features](#key-features)
- [ODDB Editor](#oddb-editor)
- [Usage](#usage)
- [Installation](#installation)

## Key Features
### Table Editor
<img width="1041" height="637" alt="image" src="https://github.com/user-attachments/assets/8124d6aa-6860-4b76-bfb4-c3a101744cfe" />

ODDB provides a table editor that allows you to create and edit tables within the Unity editor.
This enables developers to easily manage data without the need for separate database management tools.
You can open the database editing window by clicking 'ODDB/ODDB Editor' from the top menu.

### View Creation Feature
Views in ODDB define the data structure for specific groups of tables.
Created Views have the ability to enforce their child tables to inherit their characteristics as sub-characteristics.
This allows developers to access and manipulate database data in an object-oriented manner.
- For example, let's assume there is a table called `Item`, and two sub-tables called `Weapon` and `Armor`.
  Both `Weapon` and `Armor` inherit the characteristics of `Item`.
  In this case, `Weapon` and `Armor` can have their own unique characteristics (e.g., attack power, defense) while sharing common characteristics of `Item` (e.g., name, price).
- Additionally, tables that inherit from a View have their bindable class scope restricted to the parent View's Bind class and its subclasses. This is designed to maintain an object-oriented approach when using actual data.

The Bind class functionality mentioned in the example above is covered in more detail below.

### Table Creation Feature
ODDB provides the ability to create various types of tables through the table editor.
Developers can define table names, column names, and data types in the table editor.
You can also easily add/remove data.
The created tables are stored in ODDB's database and can be loaded and used at runtime.

### Bind Class Feature
ODDB provides the ability to bind tables to specific classes to access data as objects.
When initializing the database, ODDB attempts to sequentially assign each table's data to the created object's variables.
The created objects can be accessed by ID through the `ODDBPort.GetEntity<T>(string id)` method.

Note that classes used as Bind classes must inherit from the `ODDBEntity` class by default.
This is to provide the basic functionality needed for ODDB to manage and manipulate database entities.

### Synchronization with External Data Sources
ODDB provides synchronization capabilities with external data sources such as CSV files and Google Sheets.
This allows developers to collaborate with people from various roles to manage data.

#### CSV Synchronization
ODDB provides the ability to import and export table data through CSV files.
You can load data from CSV files through the `ODDB/CSV/Import from CSV` menu, and export current table data to CSV files through the `ODDB/CSV/Export to CSV` menu.

The exported tables are saved with the filename `{Table Name}_{Table ID}` and can be opened and edited in programs that support CSV, such as EXCEL.

#### Google Sheets Synchronization
ODDB also provides integration with Google Sheets. You can utilize more intuitive and useful features for data management compared to CSV.
However, to integrate ODDB with Google Sheets, you need to use Google Sheets' `Apps Script`.
For detailed Google Sheets integration instructions, please refer to the [Google Sheets Integration Guide](./Doc/GoogleSheetGuides/GoogleSheetsGuide_EN.md).

Once integration is complete, you can load data from Google Sheets through the `ODDB/Google Sheets/Import from Google Sheets` menu, and export current table data to Google Sheets through the `ODDB/Google Sheets/Export to Google Sheets` menu.

## ODDB Editor
<img width="1045" height="654" alt="image" src="https://github.com/user-attachments/assets/0ee9385b-1b9d-4afc-b05e-26cbfcbc5ee8" />

The `ODDB Editor` is one of ODDB's core features, providing an interface to create and edit tables within the Unity editor.
Through the `ODDB Editor`, developers can easily manage data without the need for separate database management tools.

### Table Creation
After launching the editor, you can create a new table by selecting `Right-click/Add/Table` in the left list.
The created table has the name `Default Name` by default and does not belong to any View by default.

To change the table name, select the table from the list and modify it in the text box at the top of the editor created on the right.

### View Creation
After launching the editor, you can create a new View by selecting `Right-click/Add/View` in the left list.
The created View has the name `Default Name` by default and does not belong to any View by default.

Similarly, to change the table name, select the View from the list and modify it in the text box at the top of the editor created on the right.

### Common Menu
<img width="792" height="106" alt="image" src="https://github.com/user-attachments/assets/1ea356cf-b20b-4549-93c5-62361a9f6549" />

When you select a created Table or View, an editor appears on the right to modify that Table/View.
At the top of the editor, you can configure several settings that are commonly used.

#### Name Change
You can change the name of the currently selected Table/View in the text box at the top of the editor.
Names do not need to be unique and can be changed at any time later.
This does not affect the use of the table as data later and is used only for identification purposes.

#### View/Table Editor Switch Feature
You can switch to the settings editor for the currently viewed Table or View by clicking the `Table/View` button at the top of the editor.
`View` is displayed as `View` by default, and `Table` is displayed as `Table` by default.
The available editors for each table type are as follows:
- `View` : `View` settings editor
- `Table` : `View` settings editor, `Table` settings editor

Each settings editor provides the following features:
- `View` settings editor : You can modify the field settings of the View.
- `Table` settings editor : You can modify the Row data of the Table.

#### Bind Class Settings
<img width="1036" height="357" alt="image" src="https://github.com/user-attachments/assets/6bfb7f8e-eb36-45e1-ba1c-9c354a5741f5" />

By default, there is a dropdown menu displayed as `Bind:None` in the toolbar at the top of the editor.
Clicking the dropdown menu shows a list of classes that can be bound to the currently selected Table/View.
Selecting a class from the list sets the class to bind to that Table/View.
The bound class is used to convert the Table/View data into objects at runtime.

If you select `None`, it means that there is no bound class for that Table/View.
When set this way, it operates the same as not using the Table/View data at runtime.

#### Inherit View Settings
<img width="1042" height="377" alt="image" src="https://github.com/user-attachments/assets/f658caf2-621c-4f23-ba2c-fc6b44a292a1" />

Similarly, there is a dropdown menu displayed as `Inherit:None` in the toolbar at the top of the editor.
Clicking the dropdown menu shows a list of Views that the currently selected `Table/View` can inherit from.
Selecting a View from the list causes the `Table/View` to inherit from the selected View.
The inherited View affects the field settings of the `Table/View` afterward. These effects are reflected immediately.

If you select `None`, it means that the Table/View does not inherit from any View.

### `View` Settings Editor
<img width="794" height="274" alt="image" src="https://github.com/user-attachments/assets/01c720ac-a7c1-4b6d-b083-f1bc620b78cb" />

The `View` settings editor is available for both Views and Tables.
Since all Tables inherit from Views by default, it is possible to modify field data in the same way.

Looking at the top of the editor that appears on the right, there is a button displayed as `Table/View`. View is displayed as `View` by default, and Table is displayed as `Table`.
Selecting `View` from that button displays an editor where you can modify the Fields of the currently viewed `Table/View`.

Field settings changed in a parent View are immediately reflected in child Tables/Views.
However, field settings changed in child Tables/Views do not affect the parent View and only affect themselves.

All Field changes are managed at the Scope level.
- Fields added in a parent View are all equally added to child Tables/Views.
- Fields added in child Tables/Views do not affect the parent View and are only added to themselves.

In other words, child Tables/Views inherit all Fields from parent Views and can additionally have their own Fields.

#### Add Field
To add a Field, click the `Add Field` button.
When a new Field is added, it has the name `Default Field` and type `string` by default.
The field name and type can be modified through the text box and dropdown menu respectively.
The field type can be selected from various data types such as `string`, `int`, `float`, `bool`, etc.
It also supports some `Asset` types provided by Unity.

#### Remove Field
To remove a field, click the `-` button on the right side of that field.
When a field is removed, all data stored in that field is also deleted.

### `Table` Settings Editor
<img width="788" height="341" alt="image" src="https://github.com/user-attachments/assets/5119eaf4-ae64-4dbc-9a5a-5be11ae0da82" />

The `Table` settings editor is only available for Tables.
With a Table selected, clicking the `Table` button at the top of the right editor displays the Table settings editor.
It is set to Table by default.

#### Add Row
You can add a new Row by clicking the `Create Row` button.
When a new Row is added, all Fields defined in the parent View are included by default.
The Field data of each Row can be modified through the text box and dropdown menu of that Row.

#### Remove Row
To remove a Row, click the `-` button on the right side of that Row.
When a Row is removed, all data stored in that Row is also deleted.

## Usage
ODDB initializes the database when the game enters runtime.
Therefore, you do not need to write separate initialization code.
After the database is initialized, you can access the database through the `ODDBPort` class.

### Data Access
The ODDBPort class provides various methods for accessing the database.

#### GetEntity Method
The most basic method is the `GetEntity<T>(string id)` method, which returns data with a specific ID as an object.
- For example, let's assume that the `Item` table is bound to the `ItemData` class.
  In this case, each row of the `Item` table is converted to an instance of the `ItemData` class.
  Developers can easily access specific item data as objects using methods like `ODDBPort.GetEntity<ItemData>("item_001")`.

#### GetEntities Method
Another useful method is the `GetEntities<T>()` method, which returns all data bound to a specific class as a list of objects.
- For example, let's assume that the `Weapon` table is bound to the `WeaponData` class.
  In this case, developers can easily access all weapon data as a list of objects using a method like `ODDBPort.GetEntities<WeaponData>()`.
- As a second example, let's assume there is an `ItemData` class bound to the `Item` View, and `Weapon` and `Armor` tables that inherit from that View.
  In this case, developers can easily access a list of objects containing all data from the `Weapon` and `Armor` tables using a method like `ODDBPort.GetEntities<ItemData>()`.

---
## Installation
<img width="617" height="153" alt="image" src="https://github.com/user-attachments/assets/de3c0af9-9a4b-445f-883d-de76d1362388" />

ODDB can be installed through the Unity Package Manager.
1. Open the Unity editor and select `Window > Package Manager` from the top menu.
2. When the Package Manager window opens, click the `+` button in the upper left corner and select `Add package from git URL...`.
3. Enter `https://github.com/Postive-ToolKit/ODDB.git` in the popup window and click the `Add` button.

