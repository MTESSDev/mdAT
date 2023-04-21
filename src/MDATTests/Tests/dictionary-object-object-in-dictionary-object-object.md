# Dictionary_object_object_in_Dictionary_object_object

> Dictionary_object_object in Dictionary_object_object 

## Case 1

Description

``````yaml
dict:
  key1: 
    subkey1: 
       subsubkey: "F"
    subkey2: 2
expected:
  allowAdditionalProperties: false
  data: {"key1":{"subkey1":{"subsubkey":"F"},"subkey2":2}}
``````