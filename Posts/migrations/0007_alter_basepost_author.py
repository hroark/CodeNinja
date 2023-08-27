# Generated by Django 4.2.3 on 2023-08-24 16:54

from django.conf import settings
from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        migrations.swappable_dependency(settings.AUTH_USER_MODEL),
        ('Posts', '0006_alter_basepost_author_delete_author'),
    ]

    operations = [
        migrations.AlterField(
            model_name='basepost',
            name='author',
            field=models.ManyToManyField(related_name='Username', to=settings.AUTH_USER_MODEL),
        ),
    ]