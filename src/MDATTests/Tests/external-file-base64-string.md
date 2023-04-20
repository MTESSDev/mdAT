# External_file_base64_String

> External file include base64 string

## Case 1

Description

``````yaml
form: !include Hello World.pdf
expected:
  allowAdditionalProperties: false
  data: !include Hello World.pdf
``````