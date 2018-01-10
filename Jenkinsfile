node {
	stage 'Checkout'
		checkout scm

	stage 'Build'
		bat 'nuget restore OCR_Console.sln'
		bat "\"${tool 'MSBuild'}\" OCR_Console.sln /p:Configuration=Release /p:Platform=\"Any CPU\" /p:ProductVersion=1.0.0.${env.BUILD_NUMBER}"

	stage 'Archive'
		archive 'OCR_Windows_Service/bin/Release/**'

}