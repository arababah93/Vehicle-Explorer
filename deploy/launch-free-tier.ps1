# Launches one Amazon Linux 2023 t3.micro and prints its public URL.
# Uses the AWS CLI default profile on this machine. Does not read or write keys in the repo.
$ErrorActionPreference = "Stop"

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    throw "AWS CLI is not installed. Install AWS CLI v2, run 'aws configure' locally, then run this script again."
}

aws sts get-caller-identity | Out-Null

$region = if ($env:AWS_REGION) { $env:AWS_REGION } else { "us-east-1" }
$groupName = "vehicle-explorer-web"

$ami = aws ssm get-parameter `
    --region $region `
    --name /aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64 `
    --query Parameter.Value `
    --output text

$groupId = aws ec2 describe-security-groups `
    --region $region `
    --filters "Name=group-name,Values=$groupName" `
    --query "SecurityGroups[0].GroupId" `
    --output text

if ($groupId -eq "None" -or [string]::IsNullOrWhiteSpace($groupId)) {
    $vpcId = aws ec2 describe-vpcs `
        --region $region `
        --filters "Name=isDefault,Values=true" `
        --query "Vpcs[0].VpcId" `
        --output text

    $groupId = aws ec2 create-security-group `
        --region $region `
        --group-name $groupName `
        --description "Vehicle Explorer HTTP" `
        --vpc-id $vpcId `
        --query GroupId `
        --output text

    aws ec2 authorize-security-group-ingress `
        --region $region `
        --group-id $groupId `
        --protocol tcp `
        --port 80 `
        --cidr 0.0.0.0/0 | Out-Null
}

$userData = (Resolve-Path (Join-Path $PSScriptRoot "ec2-user-data.sh")).Path -replace '\\', '/'

$instanceId = aws ec2 run-instances `
    --region $region `
    --image-id $ami `
    --instance-type t3.micro `
    --security-group-ids $groupId `
    --user-data "file://$userData" `
    --tag-specifications "ResourceType=instance,Tags=[{Key=Name,Value=vehicle-explorer}]" `
    --query "Instances[0].InstanceId" `
    --output text

Write-Host "Instance $instanceId is starting in $region."

aws ec2 wait instance-running --region $region --instance-ids $instanceId

$publicDns = aws ec2 describe-instances `
    --region $region `
    --instance-ids $instanceId `
    --query "Reservations[0].Instances[0].PublicDnsName" `
    --output text

Write-Host "Live URL: http://$publicDns"
Write-Host "The first boot builds both images on the instance. Give it about 15 minutes, then open the URL."
Write-Host "Stop it with: aws ec2 stop-instances --region $region --instance-ids $instanceId"
