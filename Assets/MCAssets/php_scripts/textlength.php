<?php
require("dbconnect.php");
$sql = "Select contents.* from contents";

$result = mysqli_query($conn, $sql) or die("2:name check query failed");
if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}

?>
<!DOCTYPE html>
<html>
<head>
	<link rel="stylesheet" href="mystyle.css">
	<title></title>
</head>
<body >
<table border="1"> 

	<tr>
    <td>
      Content Title
    </td>

      <td>
        Description
      </td>
            <td>
        Length of title
      </td>
            <td>
        length of body
      </td>
           

</tr>
<tr>
    <?php foreach($dbdata as $item): 
        echo "
        <tr><td>
        <p1>" . $item['ContentTitle'] . 
        "</a></td><td>
        <p1>" . $item['ContentBody'] . 
        "</td><td>
        <p1>" . strlen($item['ContentTitle']) . 
        "</td><td>
        <p1>". strlen($item['ContentBody']); ?>
        	</p></td><td> 
    	<?php endforeach; ?>

</tr>

</table>

</body>
</html>

?>

