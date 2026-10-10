# Test_keypairvalues

> Test object deserialization 

## Case with keyvaluepair<,>

``````yaml
input:
 - key: testK
   value: testV
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: 
        - Key: testK
          Value: testV
``````