## Deployment

### Install external Marathon LB
`dcos package install marathon-lb marathon-lb`

### Install internal Marathon LB
`dcos package install marathon-lb --options ./marathon-lb-internal-options.json`

### Create group
`dcos marathon group add group.json` 
 