# TestObjectOrException

> Test jsonPath array count

## Case 1

Description

``````yaml
formulaireWebFRW1DO:
  Exception: 
    Name: DataMisalignedException
    ObjectName: null
    ClassName: null
    Message: Test Crashed
    ParamName: null
expected:
  generateExpectedData: null
  verify: 
    - type: match
      allowAdditionalProperties: true
      data:
        ClassName: System.DataMisalignedException
``````