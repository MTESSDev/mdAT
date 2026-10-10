![Code Coverage](https://img.shields.io/badge/Code%20Coverage-92%25-success?style=flat)

# mdAT - Tests automatiques en Markdown

``mdAT`` est une bibliothèque simple qui remplace les tests unitaires et d'intégration. Avec ``mdAT``, les développeurs peuvent écrire des tests unitaires de manière traditionnelle, mais sans avoir besoin d'ajouter de nombreuses données de test à partir de fichiers JSON, XML, YAML, ou d'utiliser manuellement ``[Arguments]`` avec des objets JSON échappés.

| Paquet | Contenu |
|---|---|
| `MDAT` | Le paquet à installer : il apporte `MDAT.TUnit` et `MDAT.Core`. |
| `MDAT.TUnit` | Les sources de données pour [TUnit](https://tunit.dev) : ``[MarkdownDataSource]`` et ``[YamlFileDataSource]``. |
| `MDAT.Core` | Lecture des fichiers Markdown/YAML, ``Verify.Assert()`` et comparaison JSON. Indépendant de tout framework de test. |

Cibles : .NET 8, .NET 9 et .NET 10.

Pour utiliser mdAT, suivez ces étapes :

1. Créez un projet de test [TUnit](https://tunit.dev) (`<OutputType>Exe</OutputType>`) et ajoutez le paquet ``MDAT``.
2. Créez un test normal avec ``[Test]``.
3. Codez votre test unitaire de manière traditionnelle (avec Moq, si nécessaire), puis appelez la méthode que vous souhaitez tester.
4. Ajoutez tous les paramètres souhaités à votre méthode de test, ils seront exposés pour être remplacés par le fichier de test Markdown de ``mdAT``.
5. Ajoutez ``[MarkdownDataSource("~/Tests/{method}.md")]`` avec votre dossier ``Tests`` comme référentiel de cas (laissez-le vide au départ).
6. Assurez-vous d'entourer votre méthode testée avec ``Verify.Assert()``, cela validera automatiquement le résultat JSON attendu, même s'il s'agit d'une exception.
7. Votre fichier de test ``.md`` sera généré automatiquement lors de la première exécution.
Modifiez votre fichier ``.md`` comme vous le souhaitez.
8. Profitez-en !

Chaque cas du fichier Markdown devient un test distinct dans l'explorateur de tests, et son emplacement est son titre dans le fichier Markdown (ou le début de son document dans un fichier YAML), pas la méthode C# partagée par tous les cas. TUnit transmet cet emplacement à l'explorateur de tests, qui s'en sert pour « Aller à la source » (double-clic). Les arguments sont construits à partir du YAML et passés tels quels à votre méthode : aucune sérialisation intermédiaire, quel que soit leur type (`DateOnly`, `Dictionary<object, object>`, `JsonDocument`, ...).

````csharp
using MDAT;

public class Calc
{
    [Test]
    [MarkdownDataSource("~/Tests/{method}.md")]
    public async Task Add(int val1, int val2, string expected)
    {
        _ = await Verify.Assert(() => Task.FromResult(Add(val1, val2)), expected);
    }

    // Cas d'utilisation fictif
    public static int Add(int val1, int val2)
    {
        return val1 + val2;
    }
}
````

Tous les cas de test peuvent être stockés dans un seul fichier Markdown :

```````md
# Test de la méthode Add

Cas de test simple pour des tests d'addition.

## Cas 1

Essayons une simple addition `1 + 1` avec un résultat attendu de `2`

``````yaml
val1: 1
val2: 1
expected: 
  verify:
   - type: match
     data: 2
``````

## Case 2
``````yaml
val1: 212
val2: 2444
expected: 
  verify:
   - type: match
     data: 2656
``````
```````

Avec `mdAT`, les analystes et les développeurs peuvent travailler ensemble avec une source unique de tests, et les mainteneurs peuvent modifier et valider tous les tests localement ou sur un serveur de build.

## Options d'une étape de vérification

Chaque élément de `verify` accepte :

| Option | Défaut | Rôle |
|---|---|---|
| `type` | `match` | Type de vérification (seul `match` existe). |
| `jsonPath` | `$` | Partie du résultat à comparer. Se termine par `.length()` pour comparer une taille. |
| `data` | | Valeur attendue. |
| `allowAdditionalProperties` | `false` | Ignore les propriétés présentes dans le résultat mais absentes de `data`. |
| `ignoreArrayOrder` | `false` | Deux tableaux sont égaux s'ils contiennent les mêmes valeurs, dans n'importe quel ordre (les doublons sont comptés). S'applique à tous les niveaux. |

```yaml
expected:
  verify:
    - type: match
      jsonPath: $.items
      ignoreArrayOrder: true
      allowAdditionalProperties: true
      data:
        - A: second
        - A: first
```

## Comparaison JSON

La comparaison est faite par `MDAT.Core` (aucune dépendance à xunit ou Quibble) :

- les nombres sont comparés exactement, quelle que soit leur écriture (`1`, `1.0` et `1e0` sont égaux, `12345678901234567890` et `12345678901234567891` sont différents) ;
- les éléments d'un tableau sont appariés par valeur : un élément qui en remplace un autre est comparé en détail (`$.items[1].name`), un élément en trop ou manquant est listé avec son index ;
- une propriété en double dans un JSON (`{"a":1,"a":2}`) est refusée, au lieu d'en ignorer une silencieusement ;
- l'écart est décrit dans `JsonAssertException` (`DiffMessages`, `UserMessage`).

## Migrer vers mdAT 2.0

mdAT 2.0 abandonne MSTest au profit de TUnit. Les projets qui restent sur MSTest peuvent continuer d'utiliser mdAT 1.x.

### 1. Le projet de test

- Cibler **.NET 8 ou plus** (.NET 6 et .NET 7 ne sont plus pris en charge).
- Ajouter `<OutputType>Exe</OutputType>` : un projet TUnit est un exécutable.
- Retirer `Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, `MSTest.TestFramework` et `coverlet.collector`.
- Mettre à jour le paquet `MDAT` en 2.0 (il apporte maintenant `MDAT.TUnit` et `MDAT.Core`) et ajouter `TUnit`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TUnit" Version="1.73.19" />
    <PackageReference Include="MDAT" Version="2.0.0-beta.3" />
  </ItemGroup>
</Project>
```

Les fichiers `.md` ne changent pas et restent aux mêmes chemins (`~/` désigne toujours la racine du projet de test).

### 2. Le code des tests

| mdAT 1.x / MSTest | mdAT 2.0 / TUnit |
|---|---|
| `[TestClass]` | (retirer) |
| `[TestMethod]` | `[Test]` |
| `[MarkdownTest("~/Tests/{method}.md")]` | `[MarkdownDataSource("~/Tests/{method}.md")]` (l'ancien nom reste utilisable, voir ci-dessous) |
| `[YamlFileData("~/Tests/x.yaml")]` | `[YamlFileDataSource("~/Tests/x.yaml")]` (idem) |
| `[DataRow(...)]` | `[Arguments(...)]` |
| `[DynamicData(...)]` | `[MethodDataSource(...)]` |
| `[TestInitialize]` / `[TestCleanup]` | `[Before(Test)]` / `[After(Test)]` |
| `[ClassInitialize]` | `[Before(Class)]` |
| `[DoNotParallelize]` | `[NotInParallel]` |
| `Assert.AreEqual(a, b)` | `await Assert.That(b).IsEqualTo(a)` |

`Verify.Assert()`, `Expected`, `ObjectOrException<T>`, `ReturnsOrThrows` et l'inclusion de fichiers (`!include`) fonctionnent sans changement. Les types restent dans l'espace de noms `MDAT`.

Les noms `[MarkdownTest]` et `[YamlFileData]` continuent de fonctionner dans `MDAT.TUnit` : ce sont des alias exacts de `[MarkdownDataSource]` et `[YamlFileDataSource]`, sans avertissement. Les deux noms sont valides : vos fichiers de test n'ont à changer que pour passer de MSTest à TUnit (`[TestClass]`, `[TestMethod]`, etc.), pas pour mdAT.

### 3. Changements d'API

| mdAT 1.x | mdAT 2.0 |
|---|---|
| `MarkdownTestAttribute` | `MarkdownDataSourceAttribute` (`MDAT.TUnit`) ; la lecture des cas est dans `MarkdownTestSource.GetCases()` (`MDAT.Core`). `MarkdownTestAttribute` reste disponible, comme alias de `MarkdownDataSourceAttribute`. |
| `YamlFileDataAttribute` | `YamlFileDataSourceAttribute` ; lecture dans `YamlFileTestSource.GetCases()`. `YamlFileDataAttribute` reste disponible, comme alias de `YamlFileDataSourceAttribute`. |
| `InternalTestFailureException` (MSTest) | `MdatTestDefinitionException` |
| `Quibble.Xunit.JsonAssertException`, `JsonDiffConfig`, `JsonAssert` | `MDAT.JsonAssertException`, `MDAT.JsonDiffConfig`, `MDAT.JsonAssert` |

Les dépendances `Quibble.Xunit` (donc xunit), `MSTest.TestFramework`, `System.Data.SqlClient` et `System.Text.RegularExpressions` sont retirées, ce qui supprime leurs vulnérabilités transitives connues. `Moq` reste une dépendance de `MDAT.Core` pour les méthodes `ReturnsOrThrows`.

### 4. Convertisseurs YAML globaux

TUnit lit les cas des sources de données **pendant la découverte des tests**, avant l'exécution de tout hook de classe ou de test. Un convertisseur enregistré avec `MdatConfig.AddYamlTypeConverter()` dans un `[ClassInitialize]` / `[Before(Class)]` arrive donc trop tard. Enregistrez-le dans un hook de découverte :

```csharp
public static class Setup
{
    [Before(TestDiscovery)]
    public static void RegisterConverters()
        => MdatConfig.AddYamlTypeConverter(new MonConvertisseur());
}
```

Le convertisseur s'applique alors à tous les fichiers `.md` du projet de test.

### 5. Comparaison JSON : ce qui peut changer

Le verdict d'un test (réussi ou échoué) est le même qu'avec mdAT 1.x dans tous les cas que nous avons comparés, **sauf deux comportements devenus plus stricts**. Un test qui passait grâce à eux peut maintenant échouer :

- les nombres sont comparés exactement et non plus en `double` : `12345678901234567890` et `12345678901234567891`, ou `0.1` et `0.10000000000000001`, étaient jugés égaux et sont maintenant différents ;
- un JSON avec une propriété en double (`{"a":1,"a":2}`) est refusé, au lieu d'en ignorer une silencieusement.

Les autres changements portent sur les messages d'écart, qui sont plus précis :

- les valeurs répétées dans un tableau sont toutes listées (Quibble en oubliait et pouvait produire un message vide) ;
- les éléments de tableau sont appariés par valeur, pas par écriture (`1` et `1.0`), ni selon l'ordre des clés de leurs objets ;
- `allowAdditionalProperties` s'applique aussi aux objets contenus dans des tableaux ;
- le texte de l'exception change si votre code l'analysait (`MDAT.JsonAssertException` n'hérite plus d'une exception xunit).

Sérialisation de `System.Text.Json` : un `JsonDocument` (ou un `JsonElement`) dont la racine est une **chaîne contenant un objet ou un tableau JSON** est écrit comme cet objet ou ce tableau, ce qui permet d'attendre un objet YAML dans `data:`. C'était le comportement de mdAT 1.4. mdAT 1.5.0 et 1.5.1 l'avaient changé par erreur en chaîne, ce qui obligeait à écrire l'attendu sous forme de chaîne JSON : la 2.0 rétablit le comportement de la 1.4. Les autres chaînes (`"hello"`, `"123"`, un texte qui n'est pas du JSON valide) restent des chaînes.

### 6. Intégration continue

- Un projet TUnit s'exécute avec `dotnet run --project MyTests -f net9.0` (ou `dotnet test` avec le SDK .NET 10).
- La couverture de code change de configuration : voir la section 7.
- Si votre dépôt contient des fichiers binaires dont les tests lisent le contenu (PDF, images), déclarez-les dans `.gitattributes` (`*.pdf binary`) : sous Windows, la conversion automatique des fins de ligne les corrompt.

### 7. Couverture de code : ancienne et nouvelle configuration

Avec MSTest, la couverture reposait sur le collecteur `coverlet.collector` (ou « XPlat Code Coverage ») et sur un fichier `.runsettings`. TUnit s'exécute sur Microsoft Testing Platform, qui a sa propre extension de couverture : le `.runsettings` n'est plus lu et `coverlet.collector` ne sert plus.

| mdAT 1.x / MSTest | mdAT 2.0 / TUnit |
|---|---|
| Paquet `coverlet.collector` | Le retirer : rien à ajouter, la couverture est fournie par TUnit. |
| `dotnet test --collect:"XPlat Code Coverage" --settings .runsettings` | `dotnet run --project MyTests -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` |
| `.runsettings` : `<Format>cobertura</Format>` | Option `--coverage-output-format cobertura` |
| `.runsettings` : `<Include>MDAT</Include>` (limiter aux assemblies mesurées) | Fichier XML de réglages passé avec `--coverage-settings` (voir ci-dessous) |
| Résultat dans `TestResults/<guid>/coverage.cobertura.xml` | `bin/<Configuration>/<framework>/TestResults/coverage.cobertura.xml`, ou le dossier donné par `--results-directory` |
| `reportgenerator -reports:.\TestResults\*\coverage.cobertura.xml ...` | Inchangé (même format cobertura) : seul le chemin de `-reports:` change |

Pour limiter la mesure à vos assemblies (et exclure le projet de test lui-même), créez un fichier, par exemple `coverage.settings.xml` :

```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration>
  <CodeCoverage>
    <ModulePaths>
      <Include>
        <ModulePath>.*MonProjet\.dll$</ModulePath>
      </Include>
    </ModulePaths>
  </CodeCoverage>
</Configuration>
```

puis passez-le à la commande :

```
dotnet run --project MyTests -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings coverage.settings.xml
```

Dans un workflow GitHub, remplacez `dotnet test --collect:"XPlat Code Coverage" --settings .runsettings` par cette commande et pointez l'étape de rapport (par exemple `irongut/CodeCoverageSummary`) vers le nouveau chemin du fichier `coverage.cobertura.xml`. Le [dotnet.yml](.github/workflows/dotnet.yml) de ce dépôt en est un exemple.

### 8. Parallélisme : TUnit exécute les tests en parallèle

MSTest exécute les tests l'un après l'autre par défaut. **TUnit les exécute en parallèle**. Un projet dont les tests partagent un état statique (une fabrique `static`, une base de données de test, un fichier, une variable globale) peut donc se mettre à échouer sans que mdAT ni le test n'aient changé. Retour d'expérience d'une migration de 428 tests : 228 échecs causés par une fabrique statique partagée.

Deux solutions :

- isoler l'état (une instance par test, par exemple avec `[Before(Test)]`) ;
- ou désactiver le parallélisme pour tout le projet de test, avec un attribut d'assembly :

```csharp
[assembly: NotInParallel]
```

Pour ne sérialiser que certains tests, placez `[NotInParallel]` sur la classe ou la méthode (équivalent de `[DoNotParallelize]`).

---

# mdAT - Markdown Auto-Tests

``mdAT`` is a simple library that replaces unit and integration tests. With ``mdAT``, developers can write unit tests in the traditional way, but without the need to add many test data from JSON, XML, YAML files, or use manual ``[Arguments]`` with escaped JSON object.

| Package | Content |
|---|---|
| `MDAT` | The package to install: it brings `MDAT.TUnit` and `MDAT.Core`. |
| `MDAT.TUnit` | The data sources for [TUnit](https://tunit.dev): ``[MarkdownDataSource]`` and ``[YamlFileDataSource]``. |
| `MDAT.Core` | Markdown/YAML parsing, ``Verify.Assert()`` and JSON comparison. Independent from any test framework. |

Targets: .NET 8, .NET 9 and .NET 10.

To use ``mdAT``, follow these steps:

1. Create a [TUnit](https://tunit.dev) test project (`<OutputType>Exe</OutputType>`) and add the ``MDAT`` package.
1. Create a normal test with `[Test]`.
1. Code your unit test in the traditional way (using `Moq`, if needed), then call your method you want to test.
1. Add all your desired parameters to your test method, theses will be exposed to be replaced by ``mdAT`` markdown test file.
1. Add `[MarkdownDataSource("~/Tests/{method}.md")]` with your `Tests` folder as cases repository (let it being empty in first place).
1. Ensure to surround your tested method with `Verify.Assert()`, it will automatically validate the JSON expected result, even if it is an exception.
1. Your `.md` test file will be autogenerated during the first run.
1. Edit your `.md` file as you wish.
1. Enjoy!

Each case of the markdown file is its own test in the test explorer, and its location is its title in the markdown file (or the start of its document in a YAML file), not the C# method shared by all the cases. TUnit hands this location to the test explorer, which uses it for "Go to source" (double click). Arguments are built from the YAML and handed as is to your method: no intermediate serialization, whatever their type (`DateOnly`, `Dictionary<object, object>`, `JsonDocument`, ...).

````csharp
using MDAT;

public class Calc
{
    [Test]
    [MarkdownDataSource("~/Tests/{method}.md")]
    public async Task Add(int val1, int val2, string expected)
    {
        _ = await Verify.Assert(() => Task.FromResult(Add(val1, val2)), expected);
    }

    // Fake usecase
    public static int Add(int val1, int val2)
    {
        return val1 + val2;
    }
}
````

All test cases can be stored in a single markdown file:

```````md
# Test Add method

Simple test case for addition tests.

## Case 1

Let's try simple `1 + 1` expected to return `2`

``````yaml
val1: 1
val2: 1
expected: 
  verify:
   - type: match
     data: 2
``````

## Case 2
``````yaml
val1: 212
val2: 2444
expected: 
  verify:
   - type: match
     data: 2656
``````
```````

Using `mdAT`, analysts and developers can work together with a unique source of tests, and maintainers can edit and validate all tests locally or on a build server.

## Verification step options

Each `verify` item accepts:

| Option | Default | Purpose |
|---|---|---|
| `type` | `match` | Kind of verification (only `match` exists). |
| `jsonPath` | `$` | Part of the result to compare. Ends with `.length()` to compare a size. |
| `data` | | Expected value. |
| `allowAdditionalProperties` | `false` | Ignores properties found in the result but not in `data`. |
| `ignoreArrayOrder` | `false` | Two arrays are equal when they hold the same values, in any order (duplicates are counted). Applies at every level. |

```yaml
expected:
  verify:
    - type: match
      jsonPath: $.items
      ignoreArrayOrder: true
      allowAdditionalProperties: true
      data:
        - A: second
        - A: first
```

## JSON comparison

The comparison is done by `MDAT.Core` (no dependency on xunit or Quibble):

- numbers are compared exactly whatever their spelling (`1`, `1.0` and `1e0` are equal, `12345678901234567890` and `12345678901234567891` are different);
- array items are paired by value: an item replacing another one is compared in depth (`$.items[1].name`), an extra or missing item is listed with its index;
- a duplicated property in a JSON document (`{"a":1,"a":2}`) is rejected instead of silently ignoring one of them;
- the difference is described in `JsonAssertException` (`DiffMessages`, `UserMessage`).

## Migrating to mdAT 2.0

mdAT 2.0 drops MSTest in favour of TUnit. Projects staying on MSTest can keep using mdAT 1.x.

### 1. The test project

- Target **.NET 8 or later** (.NET 6 and .NET 7 are no longer supported).
- Add `<OutputType>Exe</OutputType>`: a TUnit project is an executable.
- Remove `Microsoft.NET.Test.Sdk`, `MSTest.TestAdapter`, `MSTest.TestFramework` and `coverlet.collector`.
- Update the `MDAT` package to 2.0 (it now brings `MDAT.TUnit` and `MDAT.Core`) and add `TUnit`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TUnit" Version="1.73.19" />
    <PackageReference Include="MDAT" Version="2.0.0-beta.3" />
  </ItemGroup>
</Project>
```

The `.md` files do not change and stay at the same paths (`~/` still means the root of the test project).

### 2. Test code

| mdAT 1.x / MSTest | mdAT 2.0 / TUnit |
|---|---|
| `[TestClass]` | (remove) |
| `[TestMethod]` | `[Test]` |
| `[MarkdownTest("~/Tests/{method}.md")]` | `[MarkdownDataSource("~/Tests/{method}.md")]` (the old name still works, see below) |
| `[YamlFileData("~/Tests/x.yaml")]` | `[YamlFileDataSource("~/Tests/x.yaml")]` (same) |
| `[DataRow(...)]` | `[Arguments(...)]` |
| `[DynamicData(...)]` | `[MethodDataSource(...)]` |
| `[TestInitialize]` / `[TestCleanup]` | `[Before(Test)]` / `[After(Test)]` |
| `[ClassInitialize]` | `[Before(Class)]` |
| `[DoNotParallelize]` | `[NotInParallel]` |
| `Assert.AreEqual(a, b)` | `await Assert.That(b).IsEqualTo(a)` |

`Verify.Assert()`, `Expected`, `ObjectOrException<T>`, `ReturnsOrThrows` and file inclusion (`!include`) work unchanged. Types stay in the `MDAT` namespace.

The `[MarkdownTest]` and `[YamlFileData]` names keep working in `MDAT.TUnit`: they are exact aliases of `[MarkdownDataSource]` and `[YamlFileDataSource]`, without any warning. Both names are valid: your test files only have to change to go from MSTest to TUnit (`[TestClass]`, `[TestMethod]`, etc.), not because of mdAT.

### 3. API changes

| mdAT 1.x | mdAT 2.0 |
|---|---|
| `MarkdownTestAttribute` | `MarkdownDataSourceAttribute` (`MDAT.TUnit`); reading the cases is done by `MarkdownTestSource.GetCases()` (`MDAT.Core`). `MarkdownTestAttribute` remains available, as an alias of `MarkdownDataSourceAttribute`. |
| `YamlFileDataAttribute` | `YamlFileDataSourceAttribute`; reading done by `YamlFileTestSource.GetCases()`. `YamlFileDataAttribute` remains available, as an alias of `YamlFileDataSourceAttribute`. |
| `InternalTestFailureException` (MSTest) | `MdatTestDefinitionException` |
| `Quibble.Xunit.JsonAssertException`, `JsonDiffConfig`, `JsonAssert` | `MDAT.JsonAssertException`, `MDAT.JsonDiffConfig`, `MDAT.JsonAssert` |

The `Quibble.Xunit` (hence xunit), `MSTest.TestFramework`, `System.Data.SqlClient` and `System.Text.RegularExpressions` dependencies are removed, which removes their known transitive vulnerabilities. `Moq` remains a dependency of `MDAT.Core` for the `ReturnsOrThrows` methods.

### 4. Global YAML converters

TUnit reads the cases of data sources **while discovering the tests**, before any class or test hook runs. A converter registered with `MdatConfig.AddYamlTypeConverter()` in a `[ClassInitialize]` / `[Before(Class)]` is therefore too late. Register it in a discovery hook:

```csharp
public static class Setup
{
    [Before(TestDiscovery)]
    public static void RegisterConverters()
        => MdatConfig.AddYamlTypeConverter(new MyConverter());
}
```

The converter then applies to every `.md` file of the test project.

### 5. JSON comparison: what may change

A test verdict (pass or fail) is the same as with mdAT 1.x in every case we compared, **except for two behaviours that became stricter**. A test that was passing thanks to them may now fail:

- numbers are compared exactly instead of as `double`: `12345678901234567890` and `12345678901234567891`, or `0.1` and `0.10000000000000001`, were considered equal and are now different;
- a JSON document with a duplicated property (`{"a":1,"a":2}`) is rejected, instead of silently ignoring one of the values.

The other changes concern the difference messages, which are more precise:

- repeated values in an array are all listed (Quibble dropped some and could even produce an empty message);
- array items are paired by value, not by spelling (`1` and `1.0`) nor by the key order of their objects;
- `allowAdditionalProperties` also applies to objects held in arrays;
- the exception text changes if your code parsed it (`MDAT.JsonAssertException` no longer derives from an xunit exception).

`System.Text.Json` serialization: a `JsonDocument` (or a `JsonElement`) whose root is a **string holding a JSON object or array** is written as that object or array, so a YAML object can be expected in `data:`. This was the behaviour of mdAT 1.4. mdAT 1.5.0 and 1.5.1 changed it by mistake into a string, which forced the expected value to be written as a JSON string: 2.0 restores the 1.4 behaviour. Other strings (`"hello"`, `"123"`, text that is not valid JSON) stay strings.

### 6. Continuous integration

- A TUnit project runs with `dotnet run --project MyTests -f net9.0` (or `dotnet test` with the .NET 10 SDK).
- Code coverage uses a different configuration: see section 7.
- If your repository holds binary files whose content is read by tests (PDF, images), declare them in `.gitattributes` (`*.pdf binary`): on Windows, automatic line ending conversion corrupts them.

### 7. Code coverage: old and new configuration

With MSTest, coverage relied on the `coverlet.collector` collector (or "XPlat Code Coverage") and on a `.runsettings` file. TUnit runs on Microsoft Testing Platform, which has its own coverage extension: the `.runsettings` file is no longer read and `coverlet.collector` is no longer needed.

| mdAT 1.x / MSTest | mdAT 2.0 / TUnit |
|---|---|
| `coverlet.collector` package | Remove it: nothing to add, coverage is provided by TUnit. |
| `dotnet test --collect:"XPlat Code Coverage" --settings .runsettings` | `dotnet run --project MyTests -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml` |
| `.runsettings`: `<Format>cobertura</Format>` | `--coverage-output-format cobertura` option |
| `.runsettings`: `<Include>MDAT</Include>` (limit to the measured assemblies) | XML settings file passed with `--coverage-settings` (see below) |
| Result in `TestResults/<guid>/coverage.cobertura.xml` | `bin/<Configuration>/<framework>/TestResults/coverage.cobertura.xml`, or the folder given by `--results-directory` |
| `reportgenerator -reports:.\TestResults\*\coverage.cobertura.xml ...` | Unchanged (same cobertura format): only the `-reports:` path changes |

To limit the measure to your assemblies (and exclude the test project itself), create a file, for example `coverage.settings.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration>
  <CodeCoverage>
    <ModulePaths>
      <Include>
        <ModulePath>.*MyProject\.dll$</ModulePath>
      </Include>
    </ModulePaths>
  </CodeCoverage>
</Configuration>
```

then pass it to the command:

```
dotnet run --project MyTests -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings coverage.settings.xml
```

In a GitHub workflow, replace `dotnet test --collect:"XPlat Code Coverage" --settings .runsettings` with this command and point the report step (for example `irongut/CodeCoverageSummary`) to the new path of the `coverage.cobertura.xml` file. This repository's [dotnet.yml](.github/workflows/dotnet.yml) is an example.

### 8. Parallelism: TUnit runs tests in parallel

MSTest runs tests one after the other by default. **TUnit runs them in parallel**. A project whose tests share static state (a `static` factory, a test database, a file, a global variable) may therefore start failing even though neither mdAT nor the tests changed. Feedback from a 428-test migration: 228 failures caused by a shared static factory.

Two solutions:

- isolate the state (one instance per test, for example with `[Before(Test)]`);
- or turn parallelism off for the whole test project, with an assembly attribute:

```csharp
[assembly: NotInParallel]
```

To serialize only some tests, put `[NotInParallel]` on the class or the method (equivalent of `[DoNotParallelize]`).
