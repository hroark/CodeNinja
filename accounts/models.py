from django.core.validators import MinValueValidator
from django.db import models
from django.contrib.auth.models import AbstractUser

class CustomUser(AbstractUser):
    ninja_points = models.IntegerField(default=0)

    def __str__(self):
        return self.username
    
