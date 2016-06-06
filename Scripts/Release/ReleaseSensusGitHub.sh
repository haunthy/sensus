#!/bin/sh

. ./ReleaseSensusPreparation.sh

. ./ReleaseSensusResetKeys.sh

# show updates that will be committed to the repository
echo "The following differences will be committed to the GitHub repository for release."
git difftool

# commit, push to github, merge the release branch into master, and push master to github
git commit -a -m "Sensus release v$1."
git push
git checkout master
git merge $releaseBranch
git push

# if we're not releasing from develop, then any changes we just made to the release branch need to be merged into develop.
if [ "$releaseBranch" != "develop" ]; then
    git checkout develop
    git merge $releaseBranch
    git push
    git checkout master
fi

# create tag for release and push tag to repository
tag_name="Sensus-v$1"
git tag -a $tag_name -m "Tag for Sensus release v$1."
git push origin $tag_name

# draft github release based on new tag
curl -u MatthewGerber --data "{\"tag_name\": \"$tag_name\",\"target_commitish\": \"master\",\"name\": \"Sensus release v$1\",\"body\": \"Release of Sensus version $1.\",\"draft\": false,\"prerelease\": $4}" https://api.github.com/repos/predictive-technology-laboratory/sensus/releases

# switch back to release branch
git checkout $releaseBranch
