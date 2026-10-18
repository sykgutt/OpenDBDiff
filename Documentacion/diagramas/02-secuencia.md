# Diagrama de secuencia

Comparación de dos bases de datos y generación del script T-SQL de sincronización, tal como ocurre en la GUI.

## Comparación GUI (end-to-end)

En la interfaz:

- **Izquierda (Source selector):** esquema destino de la migración (estado deseado).
- **Derecha (Destination selector):** esquema origen (estado actual).
- El script transforma **derecha → izquierda**.

```mermaid
sequenceDiagram
    actor User
    participant MainForm
    participant ProgressForm
    participant Generator as SQLServerGenerator
    participant Generate as Generate.Process
    participant SQL as SqlConnection_sys
    participant Comparer as CompareDatabase
    participant Model as Database
    participant Script as SQLScriptList

    User->>MainForm: Configura conexiones y pulsa Compare
    MainForm->>MainForm: SetSourceGenerator izquierda
    MainForm->>MainForm: SetDestinationGenerator derecha
    Note over MainForm: ProgressForm recibe right como Origin<br/>y left como Destination
    MainForm->>ProgressForm: ShowDialog

    ProgressForm->>Generator: Process Origin (BD derecha)
    Generator->>Generate: Process
    Generate->>SQL: Consultas embebidas sys.*
    SQL-->>Generate: Metadatos
    Generate->>Model: BuildDependency
    Generate-->>ProgressForm: Database Origin

    ProgressForm->>Generator: Process Destination (BD izquierda)
    Generator->>Generate: Process
    Generate->>SQL: Consultas embebidas sys.*
    SQL-->>Generate: Metadatos
    Generate-->>ProgressForm: Database Destination

    ProgressForm->>ProgressForm: Clone Origin
    ProgressForm->>Comparer: Compare Origin Destination
    Comparer->>Model: Marca ObjectStatus en Origin
    Comparer-->>ProgressForm: Origin mutado como Destination
    ProgressForm->>ProgressForm: Restaura Origin desde clone
    ProgressForm-->>MainForm: Origin y Destination

    MainForm->>Model: Destination.ToSqlDiff selectedSchemas
    Model->>Model: Validar seleccion y capacidades de memoria
    alt Generacion bloqueada
        Model-->>MainForm: ScriptGenerationException con motivos
        MainForm->>User: Arbol separado y motivos sin script parcial
    else Generacion permitida
        Model->>Script: SQLScript por objeto
        Script->>Script: Sort por Deep Action Dependencies
        Script-->>MainForm: ToSQL
        MainForm->>User: Script Scintilla arbol DiffPlex
    end
```

## Carga de un esquema (`Generate.Process`)

```mermaid
sequenceDiagram
    participant Generate
    participant GenerateDatabase
    participant Fillers as GenerateTables_Views_SPs
    participant SQL as SqlClient
    participant Database

    Generate->>Database: new Database Options Name
    Generate->>GenerateDatabase: Get
    GenerateDatabase->>SQL: Version collation edicion
    SQL-->>GenerateDatabase: DatabaseInfo
    GenerateDatabase-->>Generate: Info

    Generate->>Fillers: Fill Rules Tables Views
    Fillers->>SQL: TableSQLCommand y SQLQueries
    SQL-->>Fillers: Filas de catalogo
    Fillers->>Database: SchemaList Add

    alt No es Azure
        Generate->>Fillers: Partitions FileGroups Assemblies FullText
    end

    Generate->>Fillers: SPs Functions Triggers Users ExtendedProperties
    Generate->>Database: BuildDependency
    Generate-->>Generate: return Database
```

## Algoritmo de comparación (`CompareBase`)

```mermaid
sequenceDiagram
    participant CompareDatabase
    participant CompareBase
    participant OriginList as originFields
    participant DestList as destinationFields

    CompareDatabase->>CompareBase: GenerateDifferences origin dest

    loop Cada objeto en destination
        CompareBase->>OriginList: Contains FullName
        alt No existe en origin
            CompareBase->>OriginList: DoNew Status Create
        else Existe
            CompareBase->>CompareBase: DoUpdate compara propiedades
        end
    end

    loop Cada objeto en origin
        CompareBase->>DestList: Contains FullName
        alt No existe en destination
            CompareBase->>OriginList: DoDelete Status Drop
        end
    end

    CompareDatabase-->>CompareDatabase: return origin marcado
```

## Flujo CLI

```mermaid
sequenceDiagram
    actor Operator
    participant CLI as OpenDBDiff.CLI
    participant Parser as CommandLineParser
    participant Generate
    participant SQL as SqlConnection

    Operator->>CLI: -b before -a after -o archivo.sql
    CLI->>Parser: ParseArguments
    CLI->>SQL: TestConnection before
    CLI->>SQL: TestConnection after
    CLI->>Generate: Process before
    Generate-->>CLI: origin
    CLI->>Generate: Process after
    Generate-->>CLI: destination
    CLI->>Generate: Compare origin destination
    Generate-->>CLI: origin con ObjectStatus
    CLI->>CLI: ToSqlDiff ToSQL
    alt outputfile informado
        CLI->>Operator: Escribe archivo
    else
        CLI->>Operator: stdout
    end
```
