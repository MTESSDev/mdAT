# ObtenirScriptsJS

> ObtenirScriptsJS - Tests simples de configuration


### Domaine par preremplissage

``````yaml
# Fichier "Form" de configuration niveau global
configurationGlobale:
# Fichier "Form" de configuration niveau formulaire
configurationFormulaire:
# Fichier "Form" de configuration niveau système
configurationSysteme:
# Préremplissage
preRemplissage:
    domaines: 
      sports:
        Badminton:
          label: 
            fr: Badminton
            en: Badminton
          v-if: this.val('Filtre2') !== 'Ballon'
          mots-cle:       
            fr: volant
            en: volant
# Type de scripts à obtenir, vide, jsServeur, method, watch ou computed
type:
# Résultat attendu
attendu:
  allowAdditionalProperties: false
  data: |
    function domaine_sports() { 
     const domaine = [] 
    if(this.val('Filtre2') !== 'Ballon') {
    domaine.push({value:"Badminton", "label":"Badminton", "attrs": {"mots-cle":"volant"}})

    } return domaine } 

``````