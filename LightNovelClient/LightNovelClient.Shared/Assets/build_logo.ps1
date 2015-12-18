
$inkscapeExe = "C:\Program Files\Inkscape\inkscape.exe"
<#
$scale = 1

24,30,44,50,70,71,150,310 | %{
	$pngName = "Logo-$($_).scale-$($scale*100).png"
	$size = [math]::round(($_)*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $size -h $size "Logo.svg"
}

$scale = 1.4

24,30,44,50,70,71,150,310 | %{
	$pngName = "Logo-$($_).scale-$($scale*100).png"
	$size = [math]::round(($_)*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $size -h $size "Logo.svg"
}

$scale = 2.4

24,44,50,71,150 | %{
	$pngName = "Logo-$($_).scale-$($scale*100).png"
	$size = [math]::round(($_)*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $size -h $size "Logo.svg"
}

$scale = 0.8

24,30,50,70,150,310 | %{
	$pngName = "Logo-$($_).scale-$($scale*100).png"
	$size = [math]::round(($_)*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $size -h $size "Logo.svg"
}

$scale = 1.8

24,30,50,70,150,310 | %{
	$pngName = "Logo-$($_).scale-$($scale*100).png"
	$size = [math]::round(($_)*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $size -h $size "Logo.svg"
}
#>

<#
1.0,1.25,1.5,2.0,4.0 | %{
	$scale = $_
	$pngName = "Logo-150.scale-$($scale*100).png"
	$wsize = [math]::round(150*$scale)
	$hsize = [math]::round(150*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "LogoSquareMedium.svg"
}
#>


<#
1.0,1.25,1.5,2.0,4.0  | %{
	$scale = $_
	$pngName = "StoreLogo.scale-$($scale*100).png"
	$wsize = [math]::round(50*$scale + 0.001)
	$hsize = [math]::round(50*$scale + 0.001)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "Logo.svg"
}#>

1.0,1.25,1.5,2.0,4.0  | %{
	$scale = $_
	$pngName = "SplashScreen.scale-$($scale*100).png"
	$wsize = [math]::round(620*$scale + 0.001)
	$hsize = [math]::round(300*$scale + 0.001)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "SplashScreen.svg"
}


<#
1.0,1.25,1.5,2.0,4.0  | %{
	$scale = $_
	$pngName = "Logo-71.scale-$($scale*100).png"
	$wsize = [math]::round(71*$scale+0.0001)
	$hsize = [math]::round(71*$scale+0.0001)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "Logo.svg"
}

1.0,1.25,1.5,2.0,4.0  | %{
	$scale = $_
	$pngName = "Logo-44.scale-$($scale*100).png"
	$wsize = [math]::round(44*$scale)
	$hsize = [math]::round(44*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "Logo.svg"
}

1.0,1.25,1.5,2.0,4.0  | %{
	$scale = $_
	$pngName = "Logo-310.scale-$($scale*100).png"
	$wsize = [math]::round(310*$scale)
	$hsize = [math]::round(310*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "LogoSquareHuge.svg"
}


1.0,1.25,1.5,2.0,4.0  | %{
	$scale = $_
	$pngName = "Logo-310x150.scale-$($scale*100).png"
	$wsize = [math]::round(310*$scale)
	$hsize = [math]::round(150*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "LogoWide.svg"
}
#>
<#
0.8,1.0,1.4,1.8,2.4 | %{
	$scale = $_
	$pngName = "Logo-310x150.scale-$($scale*100).png"
	$wsize = [math]::round(310*$scale)
	$hsize = [math]::round(150*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "LogoWide.svg"
}
#>