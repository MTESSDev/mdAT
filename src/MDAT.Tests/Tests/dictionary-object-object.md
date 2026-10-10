# Dictionary_object_object

> Dictionary_object_object

## Case 1

Description

``````yaml
dict:
 el1: test
 el2: test2
expected:
  verify:
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: {"el1":"test","el2":"test2"}
``````