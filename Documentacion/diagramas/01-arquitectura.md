# Diagrama de arquitectura

Vista de componentes, capas y dependencias de OpenDBDiff. Los diagramas se renderizan en GitHub, VS Code y cualquier visor Markdown con soporte Mermaid.

## Capas y proyectos

```mermaid
graph TB
    subgraph hosts [Hosts]
        GUI["OpenDBDiff.exe<br/>WinForms"]
        CLI["OpenDBDiff.CLI.exe<br/>Consola"]
    end

    subgraph uiLayer [Capa UI]
        SSUI["OpenDBDiff.SqlServer.Ui"]
        AbsUI["OpenDBDiff.Abstractions.Ui"]
    end

    subgraph engine [Motor de esquema]
        SSS["OpenDBDiff.SqlServer.Schema"]
        AbsS["OpenDBDiff.Abstractions.Schema"]
    end

    subgraph tests [Pruebas]
        TST["OpenDBDiff.Tests"]
    end

    subgraph external [Sistemas externos]
        SQL["SQL Server 2005+<br/>catálogo sys.*"]
        Lite["LiteDB<br/>settings.liteDb"]
    end

    GUI --> SSS
    GUI --> SSUI
    GUI --> AbsUI
    GUI --> AbsS
    GUI --> Lite
    SSUI --> SSS
    SSUI --> AbsUI
    SSUI --> AbsS
    CLI --> SSS
    CLI --> AbsS
    SSS --> AbsS
    SSS --> SQL
    AbsUI --> AbsS
    TST --> SSS
    TST --> AbsS
```

## Dependencias de paquetes NuGet

```mermaid
graph LR
    GUI["OpenDBDiff GUI"]
    CLI["OpenDBDiff.CLI"]
    SSS["SqlServer.Schema"]
    TST["Tests"]

    GUI --> DiffPlex["DiffPlex 1.6.3"]
    GUI --> Scintilla["ScintillaNET 3.6.3"]
    GUI --> LiteDB["LiteDB 5.0.9"]
    GUI --> InputBox["Essy.Tools.InputBox 1.0.0"]
    CLI --> CLP["CommandLineParser 2.8.0"]
    SSS --> TSQL["TSQL.Parser 1.5.2"]
    TST --> Moq["Moq 4.7.145"]
    TST --> MSTest["MSTest"]
```

## Contratos de extensibilidad UI

La selección del proveedor usa interfaces y un handler registrado (`SQLServerProjectHandler`). La clasificación de tablas en memoria y sus motivos de bloqueo usan también una referencia directa al motor SQL Server.

```mermaid
classDiagram
    class IProjectHandler {
        <<interface>>
        +CreateSourceSelector() IFront
        +CreateDestinationSelector() IFront
        +SetSourceGenerator() IGenerator
        +SetDestinationGenerator() IGenerator
        +GetDatabaseComparer() IDatabaseComparer
        +CreateOptionControl() OptionControl
    }

    class IFront {
        <<interface>>
        +ConnectionString string
        +TestConnection()
        +Control Control
    }

    class IGenerator {
        <<interface>>
        +Process() IDatabase
        +OnProgress
    }

    class IDatabaseComparer {
        <<interface>>
        +Compare(origin, destination) IDatabase
    }

    class SQLServerProjectHandler
    class SqlServerConnectFront
    class SQLServerGenerator
    class SQLServerComparer

    IProjectHandler <|.. SQLServerProjectHandler
    IFront <|.. SqlServerConnectFront
    IGenerator <|.. SQLServerGenerator
    IDatabaseComparer <|.. SQLServerComparer
    SQLServerProjectHandler --> SqlServerConnectFront
    SQLServerProjectHandler --> SQLServerGenerator
    SQLServerProjectHandler --> SQLServerComparer
```

## Modelo de dominio (resumen)

```mermaid
classDiagram
    class ISchemaBase {
        <<interface>>
        +Status ObjectStatus
        +ObjectType ObjectType
        +FullName string
        +ToSql() string
        +ToSqlDiff() SQLScriptList
        +Clone() ISchemaBase
    }

    class SchemaBase
    class SQLServerSchemaBase
    class IDatabase
    class Database
    class Table
    class Code
    class StoredProcedure
    class View
    class Function

    ISchemaBase <|.. SchemaBase
    SchemaBase <|-- SQLServerSchemaBase
    IDatabase <|.. Database
    SQLServerSchemaBase <|-- Database
    SQLServerSchemaBase <|-- Table
    SQLServerSchemaBase <|-- Code
    Code <|-- StoredProcedure
    Code <|-- View
    Code <|-- Function
    Database o-- Table
    Database o-- StoredProcedure
    Database o-- View
    Database o-- Function
```
