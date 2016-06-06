#!/bin/sh

. ./ReleaseSensusCheckArgs.sh

# get name of release branch -- this is the current branch
releaseBranch=$(git rev-parse --abbrev-ref HEAD)

# grab latest commit on the release branch
git pull

# set encryption key -- can be generated with `uuidgen`
sed -i '' "s/private const string ENCRYPTION_KEY = \"\"/private const string ENCRYPTION_KEY = \"$5\"/g" ../../SensusService/SensusServiceHelper.cs

# set xamarin insights key to production value
sed -i '' "s/protected const string XAMARIN_INSIGHTS_APP_KEY = \"\"/protected const string XAMARIN_INSIGHTS_APP_KEY = \"$6\"/g" ../../SensusService/SensusServiceHelper.cs

