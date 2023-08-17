import datetime
from enum import Enum
from django.db import models

class postType(Enum):
    request = 1
    code = 2



class basePost(models.Model):
    title = models.CharField(max_length=50)
    text = models.CharField(max_length=250)
    createDate = models.DateTimeField("Date created")
    language = models.CharField(max_length=10)
    ##author = models.ForeignKey('User', on_delete=models.CASCADE)
    pType = postType
    
    def __str__(self) -> str:
        return self.Title
    
class requestPost(basePost):
    pType = postType.request
        
class codePost(basePost):
    ninjaPoints = models.IntegerField(default=0)
    pType = postType.code
    
    
