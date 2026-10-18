# Diagramas de flujo

## Flujo de usuario (GUI)

```mermaid
flowchart TD
    Start([Inicio MainForm]) --> Load[Carga SQLServerProjectHandler]
    Load --> Restore[Restaura ultima config LiteDB]
    Restore --> Connect[Usuario elige servidor auth y BD]
    Connect --> Options{Ajusta opciones?}
    Options -->|Si| OptForm[OptionForm SqlOptionsFront]
    OptForm --> Connect
    Options -->|No| Compare[Pulsa Compare]
    Compare --> Progress[ProgressForm carga ambas BD]
    Progress --> Diff[CompareDatabase marca ObjectStatus]
    Diff --> Validate{Seleccion permite generar SQL?}
    Validate -->|Si| Script[ToSqlDiff genera T-SQL]
    Validate -->|No| Block[Explica bloqueos sin script]
    Script --> Show[Schema separa Tables y Memory-Optimized Tables]
    Block --> Show
    Show --> Action{Siguiente accion}
    Action -->|Seleccionar objetos| Filter[Recalcula ToSqlDiff]
    Filter --> Show
    Action -->|Guardar script| Save[btnSaveAs archivo.sql]
    Action -->|Aplicar objeto| Updater[Updater SqlCommand]
    Action -->|Comparar datos| Data[DataCompareForm]
    Action -->|Guardar proyecto| Proj[Project.Upsert LiteDB]
    Save --> EndNode([Fin])
    Updater --> EndNode
    Data --> EndNode
    Proj --> Show
```

## Flujo del motor de comparación

```mermaid
flowchart TD
    A([Generate.Process]) --> B[Crear Database y SqlOption]
    B --> C[GenerateDatabase.Get version collation]
    C --> D[Fill Rules Tables Views]
    D --> E{FilterIndex?}
    E -->|Si| F[Fill Index y FullTextIndex]
    E -->|No| G[UDT XMLSchemas Schemas]
    F --> G
    G --> H{Es Azure?}
    H -->|No| I[Partitions FileGroups Assemblies FullText]
    H -->|Si| J[DDLTriggers Synonyms]
    I --> J
    J --> K[SPs Functions Triggers TextObjects Users]
    K --> L[ExtendedProperties]
    L --> M[BuildDependency]
    M --> N([Database en memoria])

    N --> O[Repetir Process para la otra BD]
    O --> P[CompareDatabase.GenerateDifferences]
    P --> Q{Objeto solo en destino?}
    Q -->|Si| R[ObjectStatus.Create]
    Q -->|No| S{Objeto solo en origen?}
    S -->|Si| T[ObjectStatus.Drop]
    S -->|No| U[DoUpdate Alter Rebuild AlterWhitespace]
    R --> V[ToSqlDiff por tipo]
    T --> V
    U --> V
    V --> W[SQLScriptList.Sort]
    W --> X([Script T-SQL])
```

## Flujo de generación SQL por objeto

```mermaid
flowchart TD
    Obj[ISchemaBase.ToSqlDiff] --> Memory{Tabla en memoria?}
    Memory -->|Si| Safe{Prevalidacion y capacidades}
    Safe -->|Bloqueado| Reason[Motivos sin SQL parcial]
    Safe -->|Permitido| MemorySql[CREATE DROP o ALTER especifico]
    MemorySql --> List
    Memory -->|No| St{ObjectStatus}

    St -->|Create| Add[ToSqlAdd / Create]
    St -->|Drop| Drop[ToSqlDrop]
    St -->|Alter o AlterWhitespace| Alter[ToSQLAlter]
    St -->|Rebuild| Rebuild[Tabla temporal sp_rename migracion]
    St -->|RebuildDependencies| Deps[DROP dependencias ALTER CREATE dependencias]
    St -->|Disabled| CT[Change Tracking]
    St -->|Original| Skip[Sin script]

    Add --> List[SQLScriptList.Add]
    Drop --> List
    Alter --> List
    Rebuild --> List
    Deps --> List
    CT --> List
    Skip --> Out
    List --> Sort[Orden Deep ScriptAction Dependencies]
    Sort --> Out([ToSQL concatenado])
```

## Convención origen / destino

La nomenclatura cambia según la capa. Este flujo aclara el sentido del script.

```mermaid
flowchart LR
    subgraph ui [GUI]
        Left["Izquierda<br/>Source selector<br/>estado deseado"]
        Right["Derecha<br/>Destination selector<br/>estado actual"]
    end

    subgraph engine [Motor CompareDatabase]
        Origin["origin<br/>antes de aplicar acciones"]
        Dest["destination<br/>despues de aplicar acciones"]
    end

    subgraph cli [CLI]
        Before["--before<br/>estado anterior"]
        After["--after<br/>estado deseado"]
    end

    Right --> Origin
    Left --> Dest
    Before --> Origin
    After --> Dest
    Origin -->|"script transforma origin hacia destination"| Dest
```
