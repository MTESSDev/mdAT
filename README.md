![Code Coverage](https://img.shields.io/badge/Code%20Coverage-92%25-success?style=flat)

# mdAT - Tests automatiques en Markdown

[![en](https://img.shields.io/badge/lang-en-red.svg)](https://github.com/MTESSDev/mdAT/blob/main/README-en.md)

## Description

`mdAT` est une bibliothèque simple qui remplace les tests unitaires et d'intégration. Avec `mdAT`, les développeurs peuvent écrire des tests unitaires de manière traditionnelle, mais sans avoir besoin d'ajouter de nombreuses données de test à partir de fichiers JSON, XML, YAML, ou d'utiliser manuellement `[InlideData]` avec des objets JSON échappés.

Pour utiliser mdAT, suivez ces étapes :

1. Créez un test MSTestV2 normal avec `[TestClass]`, `[TestMethod]`.
2. Codez votre test unitaire de manière traditionnelle en utilisant Moq, si nécessaire, puis appelez la méthode que vous souhaitez tester.
3. Ajoutez tous les paramètres souhaités à votre méthode de test, ils seront exposés pour être remplacés par le fichier de test Markdown de `mdAT`.
4. Ajoutez `[MarkdownTest("~/Tests/{method}.md")]` avec votre dossier `Tests` comme référentiel de cas (laissez-le vide au départ).
5. Assurez-vous d'entourer votre méthode testée avec `Verify.Assert()`, cela validera automatiquement le résultat JSON attendu, même s'il s'agit d'une exception.
6. Votre fichier de test `.md` sera généré automatiquement lors de la première exécution.
Modifiez votre fichier `.md` comme vous le souhaitez.
7. Profitez-en !

````csharp
[TestClass]
public class Calc
{
    [TestMethod]
    [MarkdownTest("~/Tests/{method}.md")]
    /// <summary>
    /// Cas de test simple pour des tests d'addition.
    /// </summary>
    /// <param name="val1">Valeur 1</param>
    /// <param name="val2">Valeur 2</param>
    /// <param name="expected">Résultat attendu</param>
    /// <returns></returns>
    public async Task Add(int val1, int val2, Expected expected)
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

> Cas de test simple pour des tests d'addition.

## Cas 1

Essayons une simple addition `1 + 1` avec un résultat attendu de `2`

``````yaml
# Valeur 1
val1: 1
# Valeur 2
val2: 1
# Résultat attendu
expected: 
  verify:
   - type: match
     data: 2
``````

## Case 2

``````yaml
# Valeur 1
val1: 212
# Valeur 2
val2: 2444
# Résultat attendu
expected: 
  verify:
   - type: match
     data: 2656
``````
```````

Avec `mdAT`, les analystes et les développeurs peuvent travailler ensemble avec une source unique de tests, et les mainteneurs peuvent modifier et valider tous les tests localement ou sur un serveur de build.

## Paramètres expected

|Paramètre|Obligatoire|Valeur par défaut|Description|
|---------|-----------|-----------------|-----------|
|name|Non|null|Permet de nommer un expected afin d'identifier lequel a échoué dans les messages d'échec des tests (utile quand le cas contient plusieurs expected).|
|generateExpectedData|Non|null|Permet de spécifier un chemin vers un fichier existant afin d'écrire le retour du test pour ce expected afin de le déboguer plus facilement.|
|verify|Oui||Contient les paramètres de validation du expected.|
|verify.type|Non|match|`match` est la seule valeur possible actuellement.|
|verify.jsonPath|Non|$|Permet de spécifier la racine de l'objet avec `$` ou une propriété spécifique avec `$.UnePropriete`. On peut aussi spécifier l'index d'une liste avec par exemple `$[3]` ou le nombre d'éléments avec `$.length()`.|
|verify.allowAdditionalProperties|Non|false|Permet d'obliger à spécifier toutes les propriétés de l'objet.|
|verify.data|Non|null|Objet à comparer (en yaml/json/base64/ect...). La section est facultative, mais c'est plus clair d'indiquer que data est null que de l'omettre. On peut aussi assigner null au expected à la place de mettre null dans verify.data afin de l'écrire avec moins de lignes (si on a besoin que du jsonPath par défaut). Par contre, le expected n'aura pas de nom dans le message d'échec du test si le cas échoue.|

## Paramètres supplémentaire

On peut exécuter un cas spécifique avec :
````md
``````yaml selected
````

Et ne pas exécuter un cas avec :
```````md
``````yaml skipped
```````

(Ne pas oublier de retirer un `selected` temporaire par après afin que les autre cas puissent s'exécuter, sinon... 😅). Et de même pour `skipped`.

## Config csproj

Pour générer automatiquement le summary de la méthode dans les cas, il faut activer GenerateDocumentationFile dans le projet.

```````md
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```````

## Exemples

Variables et constructeur :

**Test**
````csharp
[TestClass]
public class Tests
{
    private Mock<IUtilisateurDO> _mockUtilisateurDO;
    private GestionUtilisateur _gestionUtilisateur;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockUtilisateurDO = new Mock<IUtilisateurDO>();
        _gestionUtilisateur = new(_mockUtilisateurDO.Object);
    }

    ...
}
````

**Traitement**
````csharp
public class GestionUtilisateur
{
    private readonly IUtilisateurDO _utilisateurDO;
    private readonly List<Utilisateur> _utilisateurs = [ 
        new() { Id = 1, Prenom = "Chuck", Nom = "Norris" }, 
        new() { Id = 2, Prenom = "John", Nom = "Doe" }
    ];

    public GestionUtilisateur(IUtilisateurDO utilisateurDO)
    {
        _utilisateurDO = utilisateurDO;
    }

    ...
}
````

### Expected de base

On peut vérifier une valeur.

**Tests**
````csharp
/// <summary>
/// Expected de base
/// </summary>
/// <param name="id">Id de l'utilisateur</param>
/// <param name="expected">Résultat attendu</param>
/// <returns></returns>
[TestMethod]
[MarkdownTest("~/Tests/{method}.md")]
public async Task ExpectedBase(int id, Expected expected)
{
    _ = await Verify.Assert(() => Task.FromResult(_gestionUtilisateur.ObtenirNomCompletUtilisateur(id)), expected);
}
````

**GestionUtilisateur**
````csharp
public string? ObtenirNomCompletUtilisateur(int id)
{
    Utilisateur? utilisateur = _utilisateurs.Find(u => u.Id == id);

    return utilisateur is { } ? $"{utilisateur.Prenom} {utilisateur.Nom}" : null;
}
````

**Markdown**
``````yaml
# Id de l'utilisateur
id: 1
# Résultat attendu
expected:
  verify: 
    - type: match
      data: Chuck Norris
``````

### Expected objet

On peut vérifier un objet. Il peut être inscrit dans le markdown ou dans un fichier à part. Les expected peuvent être soit en yaml ou en json.

On peut omettre certaines propriétés de la validation en inscrivant true au `allowAdditionalProperties` du expected.

On peut aussi vérifier une propriété spécifique en l'inscrivant dans le `jsonPath` du expected.

**Tests**
````csharp
/// <summary>
/// Expected objet
/// </summary>
/// <param name="id">Id de l'utilisateur</param>
/// <param name="expected">Résultat attendu</param>
/// <returns></returns>
[TestMethod]
[MarkdownTest("~/Tests/{method}.md")]
public async Task ExpectedObjet(int id, Expected expected)
{
    _ = await Verify.Assert(() => Task.FromResult(_gestionUtilisateur.ObtenirUtilisateur(id)), expected);
}
````

**GestionUtilisateur**
````csharp
public Utilisateur? ObtenirUtilisateur(int id)
{
    return _utilisateurs.Find(u => u.Id == id);
}
````

**Markdown**

Yaml/Json
``````yaml
# Id de l'utilisateur
id: 1
# Résultat attendu
expected:
  verify: 
    - type: match
      data: 
        Id: 1
        Prenom: Chuck
        Nom: Norris
    - type: match
      data: {
        "Id": 1
        "Prenom": "Chuck"
        "Nom": "Norris"
      }
``````

Fichier
``````yaml
# Id de l'utilisateur
id: !include .\Fichiers\idChuck.yml
# Résultat attendu
expected:
  verify: 
    - type: match
      data: !include .\Fichiers\Chuck.yml
``````

Partiel
``````yaml
# Id de l'utilisateur
id: 1
# Résultat attendu
expected:
  verify: 
    - type: match
      allowAdditionalProperties: true
      data: 
        Prenom: Chuck
    - type: match
      jsonPath: $.Nom
      data: Norris
``````

### Expected liste d'objets

On peut vérifier une liste d'objets.

On peut aussi vérifier un élément à une position spécifique et le nombre d'éléments avec le `jsonPath` du expected.

**Tests**
````csharp
/// <summary>
/// Expected liste d'objets
/// </summary>
/// <param name="expected">Résultat attendu</param>
/// <returns></returns>
[TestMethod]
[MarkdownTest("~/Tests/{method}.md")]
public async Task ExpectedListeObjets(Expected expected)
{
    _ = await Verify.Assert(() => Task.FromResult(_gestionUtilisateur.ObtenirUtilisateurs()), expected);
}
````

**GestionUtilisateur**
````csharp
public List<Utilisateur> ObtenirUtilisateurs()
{
    return _utilisateurs;
}
````

**Markdown**
``````yaml
# Résultat attendu
expected:
  verify: 
    - type: match
      data:
        - Id: 1
          Prenom: Chuck
          Nom: Norris
        - Id: 2
          Prenom: John
          Nom: Doe
  verify: 
    - type: match
      jsonPath: $.[1]
      data:
        - Id: 2
          Prenom: John
          Nom: Doe
  verify: 
    - type: match
      jsonPath: $.length()
      data: 2
``````

### Expected multiple

On peut avoir plusieurs expected pour vérifier le retour de la modification (nombre d'éléments modifié), le nombre d'appels au module d'accès aux données et les paramètres envoyé.

Les expected sont obligatoire, donc il faut les définir dans chaque cas de test.

On peut différencier les expected dans les message d'échec des tests avec la propriété `name` du expected.

**Tests**
````csharp
/// <summary>
/// Expected multiple
/// </summary>
/// <param name="utilisateurs">Utilisateurs à modifier</param>
/// <param name="expectedRetour">Résultat attendu</param>
/// <param name="expectedNbAppels">Résultat attendu du nombre d'appels</param>
/// <param name="expectedParamsAppels">Résultat attendu des paramètres de l'appel</param>
/// <returns></returns>
[TestMethod]
[MarkdownTest("~/Tests/{method}.md")]
public async Task ExpectedMultiple(List<Utilisateur> utilisateurs, Expected expectedRetour, Expected expectedNbAppels, Expected expectedParamsAppels)
{
    int nbAppels = 0;
    List<IEnumerable<object>> paramsAppels = [];

    _mockUtilisateurDO
        .Setup(x => x.ModifierUtilisateur(It.IsAny<Utilisateur>())).ReturnsAsync(true)
        .Callback(new InvocationAction(i => { nbAppels++; paramsAppels.Add(i.Arguments.Take(1)); }));

    _mockUtilisateurDO
        .Setup(x => x.ModifierUtilisateur(null)).ReturnsAsync(false)
        .Callback(new InvocationAction(i => { nbAppels++; paramsAppels.Add(i.Arguments.Take(1)); }));

    _ = await Verify.Assert(async () => await _gestionUtilisateur.ModifierUtilisateurs(utilisateurs), expectedRetour);
    _ = await Verify.Assert(() => Task.FromResult(nbAppels), expectedNbAppels);
    _ = await Verify.Assert(() => Task.FromResult(paramsAppels), expectedParamsAppels);
}
````

**GestionUtilisateur**
````csharp
public async Task<int> ModifierUtilisateurs(List<Utilisateur> utilisateurs)
{
    int nbUtilisateurModifie = 0;

    foreach (Utilisateur utilisateur in utilisateurs)
    {
        if (await _utilisateurDO.ModifierUtilisateur(utilisateur))
            nbUtilisateurModifie++;
    }
    
    return nbUtilisateurModifie;
}
````

**Markdown**
``````yaml
# Utilisateurs à modifier
utilisateurs:
  - Id: 1
    Prenom: Charles
    Nom: Patenaude
  - Id: 2
    Prenom: Sarah
    Nom: Connor
  - null
# Résultat attendu
expectedRetour:
  name: expectedRetour
  verify: 
    - type: match
      data: 2
# Résultat attendu du nombre d'appels
expectedNbAppels:
  name: expectedNbAppels
  verify: 
    - type: match
      data: 3
# Résultat attendu des paramètres de l'appel
expectedParamsAppels:
  name: expectedParamsAppels
  verify: 
    - type: match
      data:
        -
          - Id: 1
            Prenom: Charles
            Nom: Patenaude
        -
          - Id: 2
            Prenom: Sarah
            Nom: Connor
        - 
          - null
``````

### Expected multiple (dictionnaire)

On peut avoir plusieurs expected pour vérifier le retour de la modification (nombre d'éléments modifié), le nombre d'appels au module d'accès aux données et les paramètres envoyé.

Les expected d'un dictionnaire sont facultatif car on peut les ignorer s'il sont absent, donc on n'est pas obligé de les déclarer dans chacun des cas. (Mais toujours vérifier qu'un expected est à null et n'a pas de data n'est pas inutile.)

On peut différencier les expected dans les messages d'échec des tests avec la propriété `name` du expected.

**Tests**
````csharp
/// <summary>
/// Expected multiple (dictionnaire)
/// </summary>
/// <param name="utilisateurs">Utilisateurs à modifier</param>
/// <param name="expected">Résultat attendu</param>
/// <returns></returns>
[TestMethod]
[MarkdownTest("~/Tests/{method}.md")]
public async Task ExpectedMultipleDictionnaire(List<Utilisateur> utilisateurs, Dictionary<string, Expected> expected)
{
    int nbAppels = 0;
    List<IEnumerable<object>> paramsAppels = [];

    _mockUtilisateurDO
        .Setup(x => x.ModifierUtilisateur(It.IsAny<Utilisateur>())).ReturnsAsync(true)
        .Callback(new InvocationAction(i => { nbAppels++; paramsAppels.Add(i.Arguments.Take(1)); }));

    _mockUtilisateurDO
        .Setup(x => x.ModifierUtilisateur(null)).ReturnsAsync(false)
        .Callback(new InvocationAction(i => { nbAppels++; paramsAppels.Add(i.Arguments.Take(1)); }));

    if (expected.TryGetValue("expectedRetour", out var expectedRetour))
    {
        _ = await Verify.Assert(async () => await _gestionUtilisateur.ModifierUtilisateurs(utilisateurs), expectedRetour);
    }
    else
    {
        throw new Exception("Il faut au minimum avoir un Expected dans le dictionnaire nommé 'expectedRetour'.");
    }

    if (expected.TryGetValue("expectedNbAppels", out var expectedNbAppels))
    {
        _ = await Verify.Assert(() => Task.FromResult(nbAppels), expectedNbAppels);
    }

    if (expected.TryGetValue("expectedParamsAppels", out var expectedParamsAppels))
    {
        _ = await Verify.Assert(() => Task.FromResult(paramsAppels), expectedParamsAppels);
    }
}
````

**GestionUtilisateur**
````csharp
public async Task<int> ModifierUtilisateurs(List<Utilisateur> utilisateurs)
{
    int nbUtilisateurModifie = 0;

    foreach (Utilisateur utilisateur in utilisateurs)
    {
        if (await _utilisateurDO.ModifierUtilisateur(utilisateur))
            nbUtilisateurModifie++;
    }

    return nbUtilisateurModifie;
}
````

**Markdown**
``````yaml
# Utilisateurs à modifier
utilisateurs:
  - Id: 1
    Prenom: Charles
    Nom: Patenaude
  - Id: 2
    Prenom: Sarah
    Nom: Connor
  - null
# Résultat attendu
expected:
  expectedRetour:
    name: expectedRetour
    verify: 
      - type: match
        data: 2
  expectedNbAppels:
    name: expectedNbAppels
    verify: 
      - type: match
        data: 3
  expectedParamsAppels:
    name: expectedParamsAppels
    verify: 
      - type: match
        data:
          -
            - Id: 1
              Prenom: Charles
              Nom: Patenaude
          -
            - Id: 2
              Prenom: Sarah
              Nom: Connor
          - 
            - null
``````

### Expected fichier

On peut comparer un fichier comme paramètre du test ou comme data dans le expected.

**Tests**
````csharp
/// <summary>
/// Expected fichier
/// </summary>
/// <param name="paramFichierExemple">Exemple de fichier en paramètre</param>
/// <param name="expected">Résultat attendu</param>
/// <returns></returns>
[TestMethod]
[MarkdownTest("~/Tests/{method}.md")]
public async Task ExpectedFichier(byte[] paramFichierExemple, Expected expected)
{
    _ = await Verify.Assert(() => Task.FromResult(_gestionUtilisateur.ObtenirFichierUtilisateur()), expected);
}
````

**GestionUtilisateur**
````csharp
public Stream ObtenirFichierUtilisateur()
{
    return new FileStream("Utilisateurs.txt", FileMode.Open);
}
````

**Markdown**

`!include`
``````yaml
# Exemple de fichier en paramètre
paramFichierExemple: !include ..\..\..\docMdat\docMdat\Utilisateurs.txt
# Résultat attendu
expected:
  verify: 
    - type: match
      data: !include ..\..\..\docMdat\docMdat\Utilisateurs.txt
``````
base64
``````yaml
# Exemple de fichier en paramètre
paramFichierExemple: Sm9obiBEb2UNClNhcmFoIENvbm5vcg==
# Résultat attendu
expected:
  verify: 
    - type: match
      data: Sm9obiBEb2UNClNhcmFoIENvbm5vcg==
``````

### Mock ObjectOrException

On peut utiliser un `ObjectOrException` comme retour d'un mock afin de simuler soit le retour d'une valeur ou d'une exception afin de tester comment le traitement gère son retour.

**Tests**
````csharp
    /// <summary>
    /// Expected ObjectOrException
    /// </summary>
    /// <param name="utilisateurs">Utilisateurs à modifier</param>
    /// <param name="mockUtilisateurDO">Mock du retour de UtilisateurDO</param>
    /// <param name="expected">Résultat attendu</param>
    /// <returns></returns>
    [TestMethod]
    [MarkdownTest("~/Tests/{method}.md")]
    public async Task ExpectedObjectOrException(List<Utilisateur> utilisateurs, ObjectOrException<bool> mockUtilisateurDO, Expected expected)
    {
        _mockUtilisateurDO
            .Setup(x => x.ModifierUtilisateur(It.IsAny<Utilisateur>()))
            .ReturnsOrThrows(new ObjectOrException<bool> { Exception = mockUtilisateurDO?.Exception, Value = mockUtilisateurDO is { } && mockUtilisateurDO.Value });

        _ = await Verify.Assert(() => Task.FromResult(_gestionUtilisateur.ModifierUtilisateurs(utilisateurs)), expected);
    }
````

**GestionUtilisateur**
````csharp
public int ModifierUtilisateurs(List<Utilisateur> utilisateurs)
{
    int nbUtilisateurModifie = 0
    utilisateurs.ForEach(u => { if (_utilisateurDO.ModifierUtilisateur(u)) nbUtilisateurModifie++; });

    return nbUtilisateurModifie;
}
````

**Markdown**

Succès
``````yaml
# Utilisateurs à modifier
utilisateurs:
  - Id: 1
    Prenom: Charles
    Nom: Patenaude
# Mock du retour de UtilisateurDO
mockUtilisateurDO:
  Value: true
  Exception: null
# Résultat attendu
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      data: 1
``````

Exception
``````yaml
# Utilisateurs à modifier
utilisateurs:
  - Id: 1
    Prenom: Charles
    Nom: Patenaude
# Mock du retour de UtilisateurDO
mockUtilisateurDO:
  Value: null
  Exception: 
    ClassName: System.Exception
    Message: Une erreur est survenue.
# Résultat attendu
expected:
  verify: 
    - type: match
      allowAdditionalProperties: true
      data: 
        Message: Une erreur est survenue.
``````

## Autre exemples

### Expected http

On peut vérifier le retour d'un appel http.

``````yaml
# Id
id: 1
# Résultat attendu
expected:
  expectedRetour:
    verify: 
      - type: match
        allowAdditionalProperties: true
        data:
          StatusCode: 200
          Content:
            Id: 1
            Prenom: Chuck
            Nom: Norris
``````

### Expected BD mémoire

On peut vérifier les modifications d'Entity Framework en mémoire (en utilisant par exemple JsonDiffPatch.Net pour comparer le contenu d'une table avant et après le traitement).

``````yaml
# Utilisateurs à modifier
utilisateurs:
  - Id: 1
    Prenom: Charles
    Nom: Patenaude
  - Id: null
    Prenom: Sarah
    Nom: Connor
# Résultat attendu
expected:
  expectedRetour:
    verify: 
      - type: match
        data:
          - Op: replace
            Path: /1/Prenom
            Value: Charles
          - Op: replace
            Path: /1/Nom
            Value: Patenaude
          - Op: remove
            Path: /2/Id
          - Op: remove
            Path: /2/Prenom
          - Op: remove
            Path: /2/Nom
          - Op: add
            Value:
              Id: 3
              Prenom: Sarah
              Nom: Connor
``````

### JsonDocument

On peut utiliser un `JsonDocument` comme paramètre ou expected.

``````yaml
# Utilisateur à ajouter
utilisateur: { "Prenom": "Chuck", "Nom": "Norris" }
# Résultat attendu
expected:
  expectedRetour:
    verify: 
      - type: match
        data: true
``````

### KeyValuePair

On peut utiliser un `KeyValuePair` comme paramètre ou expected.

``````yaml
# Utilisateur à modifier
utilisateur:
  key: 1
  value: Chuck Norris
# Résultat attendu
expected:
  expectedRetour:
    verify: 
      - type: match
        data: true
``````