Do your configuration only once.

1) Infrastructure as a code
2) Automated Deployment
3) External Libraries

Infrastrucutre comes from Bicep (ARM Template overlay)
Deployed by yaml pipeline in 3 steps: Test, Build&Publish, Deploy

Configuration is tokenized. Values come from Pipelines->Library->VariableGroup