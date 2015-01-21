
$inkscapeExe = "C:\Program Files\Inkscape\inkscape.exe"

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

<#
0.8,1.0,1.4,1.8,2.4 | %{
    $scale = $_
    $pngName = "Logo-310x150.scale-$($scale*100).png"
    $wsize = [math]::round(310*$scale)
    $hsize = [math]::round(150*$scale)
	& "$inkscapeExe" --export-png="$($pngName)" -w $wsize -h $hsize "LogoWide.svg"
}
#>