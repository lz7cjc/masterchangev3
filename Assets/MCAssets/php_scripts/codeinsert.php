<?php

//DB connection - change in included file
require("dbconnect.php");



$insertCode = "INSERT INTO passwordRecover (userid, code) VALUES ('8334','dgd333gdg');";
	
	echo $insertCode;
mysqli_query($conn, $insertCode) or die("4: insert player code failed" . $insertCode);
	


$conn->close();

?>